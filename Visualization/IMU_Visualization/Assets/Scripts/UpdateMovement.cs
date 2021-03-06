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

    private float[] _buffer;

    private uint _calib;

    //public Vector3 rootOffset;

    private Vector3 EulerAngles;


    void Start()
    {
        _listener = gameObject.GetComponent<TCPListener>();
        //gameObject.GetComponent<>
    }

    // Update is called once per frame
    void Update()
    {
        if (!_listener.Connected ) { return;}
        try
        {
            lock (_listener.mThreadLock)
            {
                if (_listener.IncomingDataBufferFront != null) { _buffer = _listener.IncomingDataBufferFront; }
                else return;
                if (_listener.IncomingDataBufferFront.Length < TCPListener._datasize) return;
            }
        }
        catch (Exception e)
        {
            print("Failed to read Buffer: " + e);
            return;
        }
        try
        {

            var id = (int)_buffer[0];
            //_calib = (uint)_buffer[2];

            float quatW = _buffer[1];
            float quatX = _buffer[2];
            float quatY = _buffer[3];
            float quatZ = _buffer[4];
            //var scalingFactor = (1.00 / (1 << 14));

            //float quatW, quatX, quatY, quatZ;
            //quatW = (float)scalingFactor * ((Int16)(_buffer[3] | (_buffer[4] << 8)));
            //quatX = (float)scalingFactor * ((Int16)(_buffer[5] | (_buffer[6] << 8)));
            //quatY = (float)scalingFactor * ((Int16)(_buffer[7] | (_buffer[8] << 8)));
            //quatZ = (float)scalingFactor * ((Int16)(_buffer[9] | (_buffer[10] << 8)));

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
