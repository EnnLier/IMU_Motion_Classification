# IMU_Motion_Classification
The goal of this project is to determine skateboard stunts based on IMU data. The movement of the board is measured with up to two [Bosch BNO055](#bosch_bno055) IMUs, which are each mounted on an [Adafruit-Feather-Express](#adafruit-feather-express). To evaluate the data offline, the microcontroller is connected to a PC. This project contains a windows [driver](#ble-driver), which can connect to the microcontroller via Bluetooth Low Energy. After a connection is established, the GUI provides the possibility to plot (in GUI), save (local) and stream (via TCP) the incoming data. This project also provides a live [visualization](#visualization) of the measurments in Unity.

## Hardware
### Adafruit Feather Express
For this prototype I decided to use the [Adafruit-Feather-Express](https://www.adafruit.com/product/4062) as the acting microcontroller. It comes with a built in [nrf52480](https://www.nordicsemi.com/Products/nRF52840) Bluetooth chip from Nordic Semiconductors and a fairly decent processor. 
<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Images/Adafruit_Feather_Express.jpg" alt="Adafruit Feather Express" width="600"></p>

### Bosch BNO055
The used IMU is the [Adafruit version](https://learn.adafruit.com/adafruit-bno055-absolute-orientation-sensor) of the [Bosch BNO055](https://www.bosch-sensortec.com/products/smart-sensors/bno055/) smart sensor. The sensor uses a hardware implemented fusion algorithm which uses gyroscope, accelerometer and magnetometer data to predict an absolute orientation. Since there is no need to calculate the absolute orientation for this project, the fusion algorithm changed to predict the relative orientation using only the accelerometer and the gyroscope data. This operating mode requires less power and outputs data at a higher rate of about 100Hz.
<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Images/Bosch_BNO055.jpg" alt="Bosch BNO055" width="600"></p>

### Battery
The [Adafruit Feather](#adafruit-feather-express) is able to to draw power via USB or battery over its JST connector. Is a battery connected and the microcontroller is also pluged in via the USB connector, the Battery will conveniently charge automatically until it is fully loaded. If only the battery is plugged in, the adafruit is able to read the voltage and calculate the remaining capacity. I am currently using a standard 3.7V LiPo battery with a capacity of 350mAh. The maximum runtime is yet TBD. 
<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Images/LiPo.jpg" alt="LiPo Akku 3,7V JST" width="600"></p>

## Casing
<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Casing/src/Sensor_bundle.png" alt="Stacked sensor bundle" width="600"></p>

<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Casing/src/Casing_open.png" alt="Sensorcasing" width="600"></p>

<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Casing/src/Casing_closed.png" alt="Assembled sensor and its casing"  width="600"></p>

<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Casing/src/Casing_bottom_below.png" alt="View from below"  width="600"></p>

## Adafruit Feather
This folder contains the software for the Adafruit feather, written in C++. In this project I used the [PlatformIO](https://platformio.org/) Plugin in Visual Studio Code to write and flash code to the device. Although those tasks would also be possible with the Arduino IDE, I would not recommend it. I had to perform minor tweeks in the BNO55 library (noted in deploy code for the Feather), which is easily accessible via the PlatformIO interface, but a real fight in Arduino IDE.

## BLE Driver
The BLE Driver is completely written in C# and became quite a powerful and robust tool over the time.

## Data Evaluation
Contains Matlab code to read data from .txt files. It also provides code for offline evaluation of the measurements.

## Visualization
Unity visualization of incoming IMU data

Version: unityhub://2020.3.16f1/049d6eca3c44
