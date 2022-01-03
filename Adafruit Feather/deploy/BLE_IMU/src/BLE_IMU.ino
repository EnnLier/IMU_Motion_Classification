#include <Bluefruit.h>
#include <Adafruit_BNO055.h>
#include <Adafruit_NeoPixel.h>
#include <Arduino.h>

#define CLIENT_NAME "BLE_windows"  
#define HOST_NAME "IMU"  

BLEUart bleuart;
BLEBas blebas;

bool connected = false;
bool initialized = false;

uint8_t connection_interval_min = 12; //12 * 1.25ms = 15ms
uint8_t connection_interval_max = 20; //20 * 1.25ms = 25ms

uint8_t connection_handle;

SoftwareTimer dataTimer;
// SoftwareTimer batteryTimer;

Adafruit_BNO055 bno = Adafruit_BNO055(55, 0x28);//low //Adafruit_BNO055(56, 0x29)//high//Adafruit_BNO055(55);

Adafruit_NeoPixel neopixel = Adafruit_NeoPixel(NEOPIXEL_NUM, PIN_NEOPIXEL, NEO_GRB + NEO_KHZ800);



void setup() 
{
  Serial.begin(115200);

  if(!bno.begin())
  {
    Serial.println("Ooops, no BNO055 detected ... Check your wiring or I2C ADDR!");
    while(1);
  }
  bno.setExtCrystalUse(true);

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

  int* id = new int(2); 

  // batteryTimer.begin(27,SendBattery,0,true);
  // delay(1);
  dataTimer.begin(10,SendData,0,true);

  

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
char calib = 0x00;
uint8_t bufferQuat[8];
uint8_t bufferVect[6];



void loop() 
{
    sys, gyro, accel, mag = 0;
    calib = 0x00;
    memset(bufferQuat, 0, 8);
    memset(bufferVect, 0, 6);

    bno.getCalibration(&sys, &gyro, &accel, &mag);
    calib = 0x00;
    calib = ( mag & 0x03)| ( (accel <<2)& 0x0C) | ( (gyro <<4)& 0x30)| ( (sys <<6)& 0xC0);
    bno.readLen(Adafruit_BNO055::BNO055_QUATERNION_DATA_W_LSB_ADDR, bufferQuat, 8);
    
    tmp[0] = '0';
    tmp[1] = calib;
    for(int i =0;i<8;i++)
      tmp[i+2] = bufferQuat[i];

    bno.readLen(Adafruit_BNO055::BNO055_GYRO_DATA_X_LSB_ADDR, bufferVect, 6);
    for(int i =0;i<6;i++)
      tmp[i+10] = bufferVect[i]; //11-16
    

    memcpy(&sendBuffer[0],&tmp[0],sizeof(sendBuffer));
    // Serial.print("Data: ");
    // Serial.println(tmp);

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
  if (num >= 500)
  {
    // int r1 = rand() % 100;
    // int r2 = rand() % 100;
    // int r3 = rand() % 100;
    blebas.notify(100);
    // uint32_t c = neopixel.Color(r1, r2, r3);
    // neopixel.fill(c, 0, NEOPIXEL_NUM);
    // neopixel.show();
    num = 0;
  }
  bleuart.write(sendBuffer,16);
  digitalToggle(LED_RED);
}

// void SendBattery(TimerHandle_t handle)
// { 
//   Serial.println("1");
//   Serial.println("2");
//   // int r = rand() % 100;
//   // blebas.write(r);
//   // Serial.println("2");
//   // uint32_t c = neopixel.Color(0x00, 0x00, 254);
//   // neopixel.fill(c, 0, NEOPIXEL_NUM);
//   // neopixel.show();
//   // Serial.println("3");
//   // delay(1);
//   // Serial.println("4");
//   // c = neopixel.Color(0x00, 0x00, 0x00);
//   // Serial.println("5");
//   // neopixel.fill(c, 0, NEOPIXEL_NUM);
//   // Serial.println("6");
//   // neopixel.show();
//   // Serial.println("7");

//   // digitalToggle(LED_RED);
// }

void bleuart_notify_callback(uint16_t conn_hdl, bool enabled)
{
  Serial.println("Notify ON!");
  
  // delay(1);
  // batteryTimer.start();
  dataTimer.start();
}