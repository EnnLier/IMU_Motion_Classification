using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;
//using MathNet;
using System;


public class UpdateMovement : MonoBehaviour
{
    // Start is called before the first frame update
    private TCPListener _listener;

    private byte[] _buffer;

    private uint _calib;

    public Vector3 rootOffset;

    private Vector3 EulerAngles;


    void Start()
    {
        _listener = gameObject.GetComponent<TCPListener>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!_listener.Connected ) { return;}
        try
        {
            lock(_listener.mThreadLock)
            {
                if(_listener.IncomingDataBuffer != null) {_buffer = _listener.IncomingDataBuffer; }
                else return;
            }
        }
        catch(Exception e)
        {
            print("Failed to read Buffer: " + e);
            return;
        }
        try
        {
            _calib = (uint)_buffer[1];


            var scalingFactor = (1.00 / (1 << 14));

            float quatW, quatX, quatY, quatZ;
            quatW = (float)scalingFactor * ((Int16)(_buffer[2] | (_buffer[3] << 8)));
            quatX = (float)scalingFactor * ((Int16)(_buffer[4] | (_buffer[5] << 8)));
            quatY = (float)scalingFactor * ((Int16)(_buffer[6] | (_buffer[7] << 8)));
            quatZ = (float)scalingFactor * ((Int16)(_buffer[8] | (_buffer[9] << 8)));

            //System.Numerics.Quaternion tempBodyPose = new System.Numerics.Quaternion(quatW, quatX, quatY, quatZ);
            //System.Numerics.Quaternion conjTempBodyPose = System.Numerics.Quaternion.Conjugate(tempBodyPose);

            //Quaternion BodyPose = new Quaternion(-conjTempBodyPose.Z, -conjTempBodyPose.X, -conjTempBodyPose.Y, conjTempBodyPose.W);

            Quaternion BodyPose = new Quaternion(-quatZ, -quatX, -quatY, quatW);

            EulerAngles.x = -BodyPose.eulerAngles.x;
            EulerAngles.y = BodyPose.eulerAngles.z;
            EulerAngles.z = BodyPose.eulerAngles.y;

            //print(EulerAngles/10);
            
            //var scaleVector = new Vector3((float)0.017444, (float)0.017444, (float)0.017444);

            //BodyPose = BodyPose.eulerAngles;
            //var Orientation = BodyPose.eulerAngles;
            gameObject.transform.rotation = BodyPose; //* Quaternion.Euler(rootOffset);
            //print(BodyPose);
        }
        catch(Exception e)
        {
            print("Updating Orientation Failed: " + e);
        }
    }   
}
