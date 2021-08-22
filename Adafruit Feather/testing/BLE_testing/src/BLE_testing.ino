#include <Bluefruit.h>

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

BLEUuid testUUID(UUID16_CHR_BATTERY_LEVEL);

void setup() 
{
  Serial.begin(115200);

  Bluefruit.autoConnLed(true);
  Bluefruit.configPrphBandwidth(BANDWIDTH_NORMAL);
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

  dataTimer.begin(50,SendData,0,true);

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

uint8_t data[10] = {0,0,0,0,0,0,0,0,0,0};

void loop() 
{
  delay(1000);
  Serial.println("tic..");

  for(int i = 0; i < 10; i++)
  {
    data[i] = rand() % 10;
  } 
}

void bleuart_rx_callback(uint16_t conn_hdl)
{
  //Initialize Connection Parameters after parent device establishes all connections and sends the desired operating + fusion mode
  uint32_t t_size = bleuart.available();

  char str[t_size];
  bleuart.read(str, t_size);

  Serial.println(str);

}

void SendData(TimerHandle_t handle)
{ 
  bleuart.write(data,10);
  digitalToggle(LED_RED);
}


void bleuart_notify_callback(uint16_t conn_hdl, bool enabled)
{
  Serial.println("Notify ON!");
  dataTimer.start();
}