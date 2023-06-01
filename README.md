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
This folder contains a 3D model of the prototype, which was used to design a casing for the sensor bundle. Following some pictures of the 3D model, the casing and finally the actual prototype.
https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Casing/Pictures/
<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Casing/Pictures/Sensor_bundle.png" alt="Stacked sensor bundle" width="600"></p>

<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Casing/Pictures/Casing_open.png" alt="Sensorcasing" width="600"></p>

<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Casing/Pictures/Casing_closed.png" alt="Assembled sensor and its casing"  width="600"></p>

<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Casing/Pictures/Casing_bottom_below.png" alt="View from below"  width="600"></p>

<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Images/Sensor_bundles.jpg" alt="Sensorbundle real"  width="600"></p>


## Adafruit Feather
This folder contains the software for the Adafruit feather, written in C++. In this project I used the [PlatformIO](https://platformio.org/) Plugin in Visual Studio Code to write and flash code to the device. Although those tasks would also be possible with the Arduino IDE, I would not recommend it. I had to perform minor tweeks in the BNO55 library (noted in deploy code for the Feather), which is easily accessible via the PlatformIO interface, but a real fight in Arduino IDE.

## BLE Driver
The BLE Driver is completely written in C# and became quite a powerful and robust tool over the time. The driver is embedded in a GUI, which also provides several functionalities to further handle the incoming IMU data. 

<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Images/GUI_Connected_and_Plotting_labeled.png" alt="GUI_labeled" ></p>

First, one compatible device has to be selected from the devicelist and hit the "Connect Front" or "Connect Back" button, depending on where the sensor is later mounted on the skateboard. This works respectively for a second device, although connecting one is optional. If only one device is connected, it doesn't really matter if it occupies the "Front" or "Back" slot, since these allocations solely determine the overhead of the streamed or saved data packages. 

Once the devices are connected, you can check whether you receive data by checking the "Plot Data" checkbox. The charts refresh rate does not coincide with the rate at which the data actually arrives. It is implemented using few CPU ressources and only serves as an indicator to check if there is actually data arriving. I therefore recommended to run it in parallel to streaming and/or saving, since, unlike the software side, the first version of the hardware side of this project is rather frail and the Adafruit Feather might lose connection to the IMU due to external effects.

Additionally to the incoming data, the battery values (in percent) of each connected IMU is shown at the bottom. This value is calculated by measuring the battery voltage and comparing it to a corresponding decharge curve. On the right side of the GUI there is also a list of calibration parameters of the connected IMUs. These parameters are an indicator for the quality of the IMU data. If the measurements can be trusted, make sure the system calibration parameter (sys) always shows the value 3. If this is not the case, calibrate the sensors according to this [Video](https://www.youtube.com/watch?v=Bw0WuAyGsnY&ab_channel=BoschSensortec). Initially, if no previous calibration parameters were determined, the first set will be safed locally on the Adafruit Feather. By following the calibration video, it is also possible to overwrite the previously safed values. The IMU is constantly looking for better parameters, so just follow the video if you are not satisfied with the current set and hit the "Recalibrate" button once the System calibration is back to 3 (Highlight the according device in the devicelist).  

Streaming data via TCP works asynchronously and should be able to transmit every received packet without data loss. This functionality is currently only hardcoded to work on localhost and on an also hardcoded port.

Saving the incoming data is implemented synchronously, since the asynchronous approach did not deliver smooth timestamps (BLE is not at all fit for realtime applications) and processing of packages sometimes resulted in overruns (I know I know... Windows and BLE are not capable of realtime applications, but this is the best way to describe it). The rate at which the data is safed is currently hardcoded to 10ms, which slightly undersamples the measurements (BLE rate is 7.5ms at best), but again, since BLE is not so robust and the connection interval rate varies a lot, oversampling the incoming measurements seems rather unnecessary and eats a good chunk of CPU ressources.    

## Data Evaluation
Contains Matlab code to read data from .txt files. It also provides code for offline evaluation of the measurements.

## Visualization
This visualization allows you to check if the quaternion data is interpreted correctly. This Unity project contains a raw driver, which receives the IMU measurements via TCP connection. The overall idea is to recreate the actual skateboard movement based on the IMU data. This model could also be used to test if features for the classificator are able to recreate the movement and to check how well they describe a skateboard stunt. 

<p align="center"><img src="https://github.com/EnnLier/IMU_Motion_Classification/blob/master/Images/Visualization_Skateboard.png" alt="Sensorbundle real"  width="600"></p>
Version: unityhub://2020.3.16f1/049d6eca3c44
