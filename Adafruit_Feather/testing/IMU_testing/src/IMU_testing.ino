#include <Arduino.h>
#include <bluefruit.h>
#include <Adafruit_BNO055.h>

Adafruit_BNO055 bno = Adafruit_BNO055(55, 0x28);//low //Adafruit_BNO055(56, 0x29)//high//Adafruit_BNO055(55);


void setup() {
  // put your setup code here, to run once:
  Serial.begin(115200);

  if(!bno.begin())
  {
    Serial.println("Ooops, no BNO055 detected ... Check your wiring or I2C ADDR!");
    while(1);
  }
  bno.setExtCrystalUse(true);
}
uint8_t sys, gyro, accel, mag = 0;
char calib = 0x00;
uint8_t buffer_quat[8];
char DataQuat[9] = {'A','0','0','0','1','0','1','2'};

void loop() {
    sys, gyro, accel, mag = 0;
    calib = 0x00;
    memset(buffer_quat, 0, 8);

    //Read BNO055 Data
    bno.getCalibration(&sys, &gyro, &accel, &mag);
    calib = 0x00;
    calib = ( mag & 0x03)| ( (accel <<2)& 0x0C) | ( (gyro <<4)& 0x30)| ( (sys <<6)& 0xC0);
    bno.readLen(Adafruit_BNO055::BNO055_QUATERNION_DATA_W_LSB_ADDR, buffer_quat, 8);

    DataQuat[0] = calib;//2
    for(int i =0;i<8;i++)
      DataQuat[i+1] = buffer_quat[i]; //3-10

    //Copy it to transmission Array
    //memcpy(&toSend[0],&stringBNO[0],PacketSize);
    Serial.print("Data: ");
    Serial.println(DataQuat);

    delay(10);
}