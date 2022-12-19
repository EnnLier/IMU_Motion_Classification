#include <Bluefruit.h>

#define CLIENT_NAME "BLE_windows"  
#define HOST_NAME "IMU2"  

BLEUart bleuart;
BLEBas blebas;

bool connected = false;
bool initialized = false;

uint8_t connection_interval_min = 6; //12 * 1.25ms = 15ms
uint8_t connection_interval_max = 8; //20 * 1.25ms = 25ms

uint8_t connection_handle;

SoftwareTimer dataTimer;

BLEUuid testUUID(UUID16_CHR_BATTERY_LEVEL);

void setup() 
{
  Serial.begin(115200);

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

  dataTimer.begin(5,SendData,0,true);

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

  Serial.println();
  Serial.print("Disconnected, reason = 0x"); Serial.println(reason, HEX);
}

char data[22] = {0x01,'0','0','0','1','0','1','2','0','0','1','0','1','2','0','0','1','0','1','2'};
char dataToSend[22] = {'A','0','0','0','1','0','1','2','0','0','1','0','1','2','0','0','1','0','1','2'};

void loop() 
{
  // Serial.println("tic..");

  for(int i = 0; i < 22; i++)
  {
    data[i] = (char)(rand() % 10);
  } 
  data[0] = 0x01;
  memcpy(&dataToSend,&data,22);
  
  delay(5);
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
    // isCalibrated = false;
  }
  if (str.substring(0,10).compareTo("Disconnect") == 0)
  {
    Bluefruit.disconnect(connection_handle);
  }
}

void SendData(TimerHandle_t handle)
{ 
  // Serial.println(dataToSend);
  bleuart.write(dataToSend,22);
  digitalToggle(LED_RED);
}


void bleuart_notify_callback(uint16_t conn_hdl, bool enabled)
{
  Serial.println("Notify ON!");
  dataTimer.start();
}