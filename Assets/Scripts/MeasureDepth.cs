using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;

public class MeasureDepth : MonoBehaviour
{
    public MultiSourceManager mMultiSource;

    private CameraSpacePoint[] mCameraSpacePoints = null;
    private ColorSpacePoint[] mColorSpacePoints = null;

    private KinectSensor mSensor = null;
    private CoordinateMapper mMapper = null;
    // 우리가 사용할 센서와 매퍼는 기본적으로 깊이 데이터를 색상 포인트 아래에 매핑해야합니다.
    // 왜냐하면 Kinect의 각기 다른 카메라가 서로 다른 크기의 이미지를 만듭니다.

    private readonly Vector2Int mDepthResolution = new Vector2Int(512, 424);

    private void Awake()
    {
        mSensor = KinectSensor.GetDefault(); // 센서 도트가 기본값이되고 좌표 맵퍼를 설정
        mMapper = mSensor.CoordinateMapper;

        int arraySize = mDepthResolution.x * mDepthResolution.y;

        mCameraSpacePoints = new CameraSpacePoint[arraySize];
        mColorSpacePoints = new ColorSpacePoint[arraySize];
    }

}
