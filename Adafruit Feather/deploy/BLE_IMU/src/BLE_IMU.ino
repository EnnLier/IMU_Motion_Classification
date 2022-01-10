#include <Bluefruit.h>
#include <Adafruit_BNO055.h>
#include <Adafruit_NeoPixel.h>
#include <Arduino.h>
#include <Adafruit_LittleFS.h>
#include <InternalFileSystem.h>

using namespace Adafruit_LittleFS_Namespace;

#define CLIENT_NAME "BLE_windows"  
#define HOST_NAME "IMU"  

#define FILENAME    "/CalibrationValues.bin"
#define CONTENTS    "Adafruit Little File System test file contents"  

BLEUart bleuart;
BLEBas blebas;

File file(InternalFS);

bool connected = false;
bool initialized = false;
bool isCalibrated = false;

uint8_t connection_interval_min = 6; //6 * 1.25ms = 7.5ms
uint8_t connection_interval_max = 8; // * 1.25ms = 10ms

uint8_t connection_handle;

SoftwareTimer dataTimer;
SoftwareTimer BNOWatchdog;

Adafruit_BNO055 *bno;

Adafruit_NeoPixel neopixel = Adafruit_NeoPixel(NEOPIXEL_NUM, PIN_NEOPIXEL, NEO_GRB + NEO_KHZ800);

const String BNO_System_Status[7] = {
     "Idle",
     "System Error",
     "Initializing Peripherals",
     "System Iniitalization",
     "Executing Self-Test",
     "Sensor fusio algorithm running",
     "System running without fusion algorithms"};

const String BNO_Self_test[8] = {
     "Accelerometer self test",
     "Magnetometer self test",
     "Gyroscope self test",
     "MCU self test", "", "", "", ""};

const String BNO_Error[11] = {
     "No error",
     "Peripheral initialization error",
     "System initialization error",
     "Self test result failed",
     "Register map value out of range",
     "Register map write error",
     "BNO low power mode not available for selected operat ion mode",
     "Accelerometer power mode not available",
     "Fusion algorithm configuration error",
     "Sensor configuration error"};

void setup() 
{
  Serial.begin(115200);

  InternalFS.begin();

  bno = new Adafruit_BNO055(55, 0x28);//low //Adafruit_BNO055(56, 0x29)//high//Adafruit_BNO055(55);

  if(!bno->begin())
  {
    Serial.println("Ooops, no BNO055 detected ... Check your wiring or I2C ADDR!");
    while(1);
  }
  bno->setExtCrystalUse(true);

  delay(1000);

  Serial.println("loading calibraiton...");
  load_calibration(bno);
  Serial.println("calibration loaded");

  if(bno->isFullyCalibrated())
  {
    isCalibrated = true;
  }
  
  Bluefruit.autoConnLed(true);
  Bluefruit.configPrphBandwidth(BANDWIDTH_HIGH);
  Bluefruit.begin(1,0);
  Bluefruit.setTxPower(0);  
  Bluefruit.setName(HOST_NAME);

  Bluefruit.Periph.setConnectCallback(onConnect); //called on CONNECT
  Bluefruit.Periph.setDisconnectCallback(onDisconnect); //called on DISCONNECT
  Bluefruit.Periph.setConnInterval(connection_interval_min , connection_interval_max);

  byte uart_uuid[] ={0x9E, 0xCA, 0xDC, 0x24, 0x0E, 0xE5, 0xA9, 0xE0, 0x93, 0xF3, 0xA3, 0xB5, 0x08, 0x00, 0x40, 0x6E};
  byte bat_uuid[] = {0x9E, 0xCA, 0xDC, 0x24, 0x0E, 0xE5, 0xA9, 0xE0, 0x93, 0xF3, 0xA3, 0xB5, 0x04, 0x00, 0x40, 0x6E};
 
  bleuart.setUuid(uart_uuid);
  bleuart.begin();
  bleuart.setRxCallback(bleuart_rx_callback); //called when data is RECEIVED
  bleuart.setNotifyCallback(bleuart_notify_callback);

  blebas.setUuid(bat_uuid);
  blebas.begin();
  blebas.notify(100);

  // int* id = new int(2); 

  BNOWatchdog.begin(3000,getStatus,0,true);
  // delay(1);
  dataTimer.begin(10,SendData,0,true);

  // BNOWatchdog.start();
  bno->setMode(Adafruit_BNO055::OPERATION_MODE_IMUPLUS);//adafruit_bno055_opmode_t mode);

  neopixel.begin();

  Serial.println("Advertising");
  advertise();
}

void advertise()
{
  Bluefruit.Advertising.addFlags(BLE_GAP_ADV_FLAGS_LE_ONLY_GENERAL_DISC_MODE);
  Bluefruit.Advertising.addTxPower();
  Bluefruit.Advertising.addService(bleuart);
  if(!Bluefruit.Advertising.addName())
   Bluefruit.ScanResponse.addName();
  
  Bluefruit.Advertising.restartOnDisconnect(true);
  Bluefruit.Advertising.setInterval(32, 244);
  Bluefruit.Advertising.setFastTimeout(20);
  Bluefruit.Advertising.start(0); 

}


void onConnect(uint16_t conn_handle)
{
  connection_handle  = conn_handle;
  // Get the reference to current connection
  BLEConnection* connection = Bluefruit.Connection(conn_handle);

  char central_name[32] = { 0 };
  connection->getPeerName(central_name, sizeof(central_name));

  Serial.print("Connected to ");
  Serial.println(central_name);

  connected = true;
}

void onDisconnect(uint16_t conn_handle, uint8_t reason)
{
  (void) conn_handle;
  (void) reason;

  dataTimer.stop();
  // batteryTimer.stop();

  delay(10);
  digitalWrite(LED_RED,0);

  Serial.println();
  Serial.print("Disconnected, reason = 0x"); Serial.println(reason, HEX);
}

char sendBuffer[16] = {'A','0','0','0','1','0','1','2','0','0','1','0','1','2'};
char tmp[16];
uint8_t sys, gyro, accel, mag = 0;
uint8_t system_status, self_test_result, system_error = 0;
char calib = 0x00;
uint8_t bufferQuat[8];
uint8_t bufferVect[6];

bool read = true;

void loop() 
{
    sys, gyro, accel, mag = 0;
    calib = 0x00;
    memset(bufferQuat, 0, 8);
    memset(bufferVect, 0, 6);



    bno->getCalibration(&sys, &gyro, &accel, &mag);
    calib = 0x00;
    calib = ( mag & 0x03)| ( (accel <<2)& 0x0C) | ( (gyro <<4)& 0x30)| ( (sys <<6)& 0xC0);
    if(!bno->readLen(Adafruit_BNO055::BNO055_QUATERNION_DATA_W_LSB_ADDR, bufferQuat, 8))   // Power cut off?
    {
      Serial.println("1");
      int errCount = 0;
      while(true)
      {
        errCount++;
        if(bno->readLen(Adafruit_BNO055::BNO055_QUATERNION_DATA_W_LSB_ADDR, bufferQuat, 8))
        {
          if(elementsEqual(bufferQuat,8) || errCount >= 50)
          {
            Serial.println("creating new BNO objekt");
            delete bno;
            bno = new Adafruit_BNO055(55, 0x28);//low //Adafruit_BNO055(56, 0x29)//high//Adafruit_BNO055(55);

            if(!bno->begin())
            {
              Serial.println("Ooops, no BNO055 detected ... Check your wiring or I2C ADDR!");
              while(1);
            }
            bno->setExtCrystalUse(true);
            bno->setMode(Adafruit_BNO055::OPERATION_MODE_IMUPLUS);

            load_calibration(bno);

            break;
          }
          else
          {
            break;
          }
        }
        delay(10);
      }
        Serial.print("Error Count: ");
        Serial.println(errCount);
      }
          
    tmp[0] = 0x00;
    tmp[1] = calib;
    for(int i =0;i<8;i++)
      tmp[i+2] = bufferQuat[i];

    bno->readLen(Adafruit_BNO055::BNO055_GYRO_DATA_X_LSB_ADDR, bufferVect, 6);
    for(int i =0;i<6;i++)
      tmp[i+10] = bufferVect[i]; //11-16

    memcpy(&sendBuffer[0],&tmp[0],sizeof(sendBuffer));
    // Serial.print("Data: ");
    // Serial.println(tmp);


    if (bno->isFullyCalibrated() && !isCalibrated)
    {
      save_calibration(bno);
      isCalibrated = true;
    }

    delay(5);

    
}

void bleuart_rx_callback(uint16_t conn_hdl)
{
  uint32_t size = bleuart.available();

  char str[size];
  bleuart.read(str, size);

  Serial.println(str);

}
int num = 0;
void SendData(TimerHandle_t handle)
{ 
  num++;
  if (num >= 5000)
  {
    blebas.notify(100);
    num = 0;
  }
  bleuart.write(sendBuffer,16);
  digitalToggle(LED_RED);
}


void getStatus(TimerHandle_t handle)
{
  bool bin[8];
  
  bno->getSystemStatus(&system_status, &self_test_result, &system_error);

  toBin(bin, self_test_result);

  Serial.print("System status: ");
  Serial.println(BNO_System_Status[system_status]);

  Serial.print("Self test result: ");
  if (self_test_result == 15)
    Serial.println("All Good!");
  else
  {
    for(int i = 0; i < 4; i++)
    {
      if(bin[i])
        Serial.print(BNO_Self_test[i]);
    }
    Serial.println();
  }

  Serial.print("System error: ");
  Serial.println(BNO_Error[system_error]);
}

void toBin(bool arr[],uint8_t n)
{
  for (int i = 7; i >= 0; --i) 
  {
      arr[i] = (n >> i) & 1;
  }
}

bool elementsEqual(uint8_t arr[],uint8_t len)
{
  for (int i = 0; i < len-1; i++) 
    if(arr[i] != arr[i++])
      return false;
  return true;
}


void bleuart_notify_callback(uint16_t conn_hdl, bool enabled)
{
  Serial.println("Notify ON!");
  
  // delay(1);
  dataTimer.start();
}

bool load_calibration(Adafruit_BNO055 *bno)
{
  file.open(FILENAME, FILE_O_READ);
  char buffer[sizeof(adafruit_bno055_offsets_t)];
  if ( file )
  {
    Serial.println(FILENAME " file exists");
    
    uint32_t readlen;
    // char buffer[64] = { 0 };
    readlen = file.read(buffer, sizeof(buffer));

    Serial.println("1");

    buffer[readlen] = 0;

    Serial.println("2");

    adafruit_bno055_offsets_t *calibration = new adafruit_bno055_offsets_t();

    Serial.println("3");

    memcpy(&calibration,buffer,sizeof(adafruit_bno055_offsets_t));

    Serial.println("4");

    try
    {
      file.close();
      print_calib(calibration);

      bno->setSensorOffsets(*calibration);
    }
    catch(...)
    {
      Serial.println("loaded offsets invalid");
    }
    
    Serial.println("5");

    file.close();
    return true;

  }
  else
  {
    return save_calibration(bno);
  }

}

void print_calib(adafruit_bno055_offsets_t *calibrations)
{
    Serial.print("accx");
    Serial.println(calibrations->accel_offset_x);
    Serial.print("accy");
    Serial.println(calibrations->accel_offset_y);
    Serial.print("accz");
    Serial.println(calibrations->accel_offset_z);
    Serial.print("acc radius");
    Serial.println(calibrations->accel_radius);
    Serial.print("gyrx");
    Serial.println(calibrations->gyro_offset_x);
    Serial.print("gyry");
    Serial.println(calibrations->gyro_offset_y);
    Serial.print("gyrz");
    Serial.println(calibrations->gyro_offset_z);
    Serial.print("magx");
    Serial.println(calibrations->mag_offset_x);
    Serial.print("magy");
    Serial.println(calibrations->mag_offset_y);
    Serial.print("magz");
    Serial.println(calibrations->mag_offset_z);
}

bool save_calibration(Adafruit_BNO055 *bno)
{
  adafruit_bno055_offsets_t *offsets = new adafruit_bno055_offsets_t();
  bno->getSensorOffsets(*offsets);

  Serial.print("Open " FILENAME " file to write ... ");

  if( file.open(FILENAME, FILE_O_WRITE) )
  {
    file.write((char*)offsets, sizeof(adafruit_bno055_offsets_t));
    file.close();
    Serial.println("OK");
    return true;
  }
  else
  {
    Serial.println("Failed!");
    return false;
  }
}