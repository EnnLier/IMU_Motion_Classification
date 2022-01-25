#include <Bluefruit.h>
#include <Adafruit_BNO055.h>
#include <Adafruit_NeoPixel.h>
#include <Arduino.h>
#include <Adafruit_LittleFS.h>
#include <InternalFileSystem.h>

// using namespace Adafruit_LittleFS_Namespace;

#define CLIENT_NAME "BLE_windows"  
#define HOST_NAME "IMU"  

#define FILENAME    "CalibrationValues.bin"

#define VBATPIN A6
   

BLEUart bleuart;
BLEBas blebas;

Adafruit_LittleFS_Namespace::File file(InternalFS);

bool connected = false;
bool initialized = false;
bool isCalibrated = false;

uint8_t connection_interval_min = 6; //6 * 1.25ms = 7.5ms
uint8_t connection_interval_max = 8; // * 1.25ms = 10ms

uint8_t connection_handle;

SoftwareTimer dataTimer;

Adafruit_BNO055 *bno;

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

  while(!bno->begin())
  {
    delay(500);
    Serial.println("BNO not detected, retrying....");
  }

  bno->setExtCrystalUse(true);

  delay(1000);
  load_calibration();
  delay(1000);
  if(bno->isFullyCalibrated())
  {
    Serial.println("apparently calibrated...");
    isCalibrated = true;
  }
  else
  {
    isCalibrated = false;
  }

  // InternalFS.format();
  // if(bno->isFullyCalibrated())
  // {
  //   Serial.println("apparently calibrated...");
  //   isCalibrated = true;
  // }
  
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
  // blebas.notify(100);

  dataTimer.begin(10,SendData,0,true);

  bno->setMode(Adafruit_BNO055::OPERATION_MODE_IMUPLUS);//adafruit_bno055_opmode_t mode);


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

  // adafruit_bno055_offsets_t offs;
  // bno->getSensorOffsets(offs);
  // Serial.println("Current Offsets: ");
  // print_calib(offs);

  // delay(50);
  // bno->getSensorOffsets(offs);
  // // delay(50);
  // Serial.println("New Offsets: ");
  // // delay(50);
  // print_calib(offs);
  // delay(50);



  connection_handle  = conn_handle;
  // Get the reference to current connection
  BLEConnection* connection = Bluefruit.Connection(conn_handle);

  char central_name[32] = { 0 };
  connection->getPeerName(central_name, sizeof(central_name));

  Serial.print("Connected to ");
  Serial.println(central_name);

  connected = true;

  // load_calibration();
  // printTreeDir("/", 0);
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

char sendBuffer[22] = {'A','0','0','0','1','0','1','2','0','0','1','0','1','2','0','0','1','0','1','2'};
char tmp[22];
uint8_t sys, gyro, accel, mag = 0;
uint8_t system_status, self_test_result, system_error = 0;
char calib = 0x00;
uint8_t bufferQuat[8];
uint8_t bufferVect[6];

bool read = true;
int errCount = 0;

void loop() 
{
  if (!connected) return;
    sys, gyro, accel, mag = 0;
    calib = 0x00;
    memset(bufferQuat, 0, 8);
    memset(bufferVect, 0, 6);
    memset(tmp, 0, 22);

    bno->getCalibration(&sys, &gyro, &accel, &mag);

    calib = 0x00;
    calib = ( mag & 0x03)| ( (accel <<2)& 0x0C) | ( (gyro <<4)& 0x30)| ( (sys <<6)& 0xC0);
 
    tmp[0] = 0x00;
    tmp[1] = calib;

    bno->readLen(Adafruit_BNO055::BNO055_QUATERNION_DATA_W_LSB_ADDR, bufferQuat, 8);          
    for(int i =0;i<8;i++)
      tmp[i+2] = bufferQuat[i];
    bno->readLen(Adafruit_BNO055::BNO055_LINEAR_ACCEL_DATA_X_LSB_ADDR, bufferVect, 6);
    for(int i =0;i<6;i++)
      tmp[i+10] = bufferVect[i]; //11-16
    bno->readLen(Adafruit_BNO055::BNO055_GYRO_DATA_X_LSB_ADDR, bufferVect, 6);
    for(int i =0;i<6;i++)
      tmp[i+16] = bufferVect[i]; //17-22

    memcpy(&sendBuffer[0],&tmp[0],sizeof(sendBuffer));

    if (bno->isFullyCalibrated() && !isCalibrated)
    {
      Serial.println("Calibration found");
      save_calibration();
      isCalibrated = true;
    }
    delay(3);
    if(elementsEqual(bufferQuat,8))   // Power cut off?
    {
      errCount++;
    }
    else
    {
      errCount = 0;
    }
    if(errCount >= 200)
    {
      Serial.println("creating new BNO objekt");
      delete bno;
      bno = new Adafruit_BNO055(55, 0x28);
      if(!bno->begin())
      {
        Serial.println("Ooops, no BNO055 detected ... Check your wiring or I2C ADDR!");
        while(!bno->begin())
        {
          delay(2);
        }
      }
      bno->setExtCrystalUse(true);
      bno->setMode(Adafruit_BNO055::OPERATION_MODE_IMUPLUS);
      load_calibration();
      errCount = 0;
    }
}

void bleuart_rx_callback(uint16_t conn_hdl)
{
  uint32_t size = bleuart.available();

  char buf[size];
  bleuart.read(buf, size);
  String str(buf);

  if (str.substring(0,11).compareTo("Recalibrate") == 0)
  {
    Serial.println("Recalibrate!");
    isCalibrated = false;
    // Serial.println("Restart Device!");
  }
}


int num = 0;
void SendData(TimerHandle_t handle)
{ 
  num++;
  if (num >= 500)
  {
    blebas.notify(get_battery_percent());
    num = 0;
  }
  bleuart.write(sendBuffer,22);
  digitalToggle(LED_RED);
}


void bleuart_notify_callback(uint16_t conn_hdl, bool enabled)
{
  Serial.println("Notify ON!");
  
  dataTimer.start();
}

bool load_calibration()
{
  Serial.println("load_calibration");
  printTreeDir("/", 0);
  file.open(FILENAME, Adafruit_LittleFS_Namespace::FILE_O_READ);
  char buffer[sizeof(adafruit_bno055_offsets_t)];
  if ( file.isOpen() )
  {
    Serial.println(FILENAME " exists");

    adafruit_bno055_offsets_t calibration;
    calibration = {};
    
    // uint32_t readlen;
    // char buffer[64] = { 0 };
    // readlen = file.read(buffer, sizeof(buffer));
    file.read(buffer, sizeof(buffer));
    // buffer[readlen];
    
    memcpy(&calibration,buffer,sizeof(adafruit_bno055_offsets_t));
    print_calib(calibration);
    bno->setSensorOffsets(calibration);
    file.close();
    
    return true;
  }
  else
  {
    Serial.println("File does not yet Exist: " FILENAME "" "  waiting on calibration...");
    return false;
  }
}

bool save_calibration()
{
  InternalFS.remove(FILENAME);
  adafruit_bno055_offsets_t offsets;
  bno->getSensorOffsets(offsets);
  
  Serial.println("save_calibration: " FILENAME " file to write ... ");
  if( file.open(FILENAME, Adafruit_LittleFS_Namespace::FILE_O_WRITE) )
  {
    file.write((char*)&offsets, sizeof(adafruit_bno055_offsets_t));
    delay(40);
    file.close();
    print_calib(offsets);
    Serial.println("Calibration saved");
    printTreeDir("/", 0);
    return true;
  }
  else
  {
    Serial.println("Failed Saving!");
    printTreeDir("/", 0);
    return false;
  }
}

uint8_t get_battery_percent()
{
  float measuredvbat = analogRead(VBATPIN);
  measuredvbat *= 2;    // we divided by 2, so multiply back
  measuredvbat *= 3.7;  // Multiply by 3.3V, our reference voltage
  measuredvbat /= 1024; // convert to voltage

  if (measuredvbat >= 4.2)
    return 100;
  else if (measuredvbat >= 4.15) 
    return 95;
  else if (measuredvbat >= 4.11) 
    return 90;
  else if (measuredvbat >= 4.08) 
    return 85;
  else if (measuredvbat >= 4.02) 
    return 80;
  else if (measuredvbat >= 3.98) 
    return 75;
  else if (measuredvbat >= 3.95) 
    return 70;
  else if (measuredvbat >= 3.91) 
    return 65;
  else if (measuredvbat >= 3.87) 
    return 60;
  else if (measuredvbat >= 3.85) 
    return 55;
  else if (measuredvbat >= 3.84) 
    return 50;
  else if (measuredvbat >= 3.82) 
    return 45;
  else if (measuredvbat >= 3.8) 
    return 40;
  else if (measuredvbat >= 3.79) 
    return 35;
  else if (measuredvbat >= 3.77) 
    return 30;
  else if (measuredvbat >= 3.75) 
    return 25;
  else if (measuredvbat >= 3.73) 
    return 20;
  else if (measuredvbat >= 3.71) 
    return 15;
  else if (measuredvbat >= 3.69) 
    return 10;
  else if (measuredvbat >= 3.61) 
    return 5;
  else if (measuredvbat >= 3.27) 
    return 0;
}


void print_calib(adafruit_bno055_offsets_t calibrations)
{
    Serial.print("accx ");
    Serial.println(calibrations.accel_offset_x);
    Serial.print("accy ");
    Serial.println(calibrations.accel_offset_y);
    Serial.print("accz ");
    Serial.println(calibrations.accel_offset_z);
    Serial.print("acc radius ");
    Serial.println(calibrations.accel_radius);
    Serial.print("gyrx ");
    Serial.println(calibrations.gyro_offset_x);
    Serial.print("gyry ");
    Serial.println(calibrations.gyro_offset_y);
    Serial.print("gyrz ");
    Serial.println(calibrations.gyro_offset_z);
    Serial.print("magx ");
    Serial.println(calibrations.mag_offset_x);
    Serial.print("magy ");
    Serial.println(calibrations.mag_offset_y);
    Serial.print("magz ");
    Serial.println(calibrations.mag_offset_z);
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
  {
    // Serial.println(arr[i] != arr[i++]);
    if(arr[i] != arr[i+1])
    {
      return false;
    } 
  }
  return true;
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




void printTreeDir(const char* cwd, uint8_t level)
{
  // Open the input folder
  Adafruit_LittleFS_Namespace::File dir(cwd, Adafruit_LittleFS_Namespace::FILE_O_READ, InternalFS);

  // Print root
  if (level == 0) Serial.println("root");
 
  // File within folder
  Adafruit_LittleFS_Namespace::File item(InternalFS);

  // Loop through the directory 
  while( (item = dir.openNextFile(Adafruit_LittleFS_Namespace::FILE_O_READ)) )
  {
    // Indentation according to dir level
    for(int i=0; i<level; i++) Serial.print("|  ");

    Serial.print("|_ ");
    Serial.print( item.name() );

    if ( item.isDirectory() )
    {
      Serial.println("/");

      // ATTENTION recursive call to print sub folder with level+1 !!!!!!!!
      // High number of MAX_LEVEL can cause memory overflow
      if ( level < 2 )
      {
        char dpath[strlen(cwd) + strlen(item.name()) + 2 ];
        strcpy(dpath, cwd);
        strcat(dpath, "/");
        strcat(dpath, item.name());
        
        printTreeDir( dpath, level+1 );
      }
    }else
    {
      // Print file size starting from position 30
      int pos = level*3 + 3 + strlen(item.name());

      // Print padding
      for (int i=pos; i<30; i++) Serial.print(' ');

      // Print at least one extra space in case current position > 50
      Serial.print(' ');
      
      Serial.print( item.size() );
      Serial.println( " Bytes");
    }

    item.close();
  }

  dir.close();
}