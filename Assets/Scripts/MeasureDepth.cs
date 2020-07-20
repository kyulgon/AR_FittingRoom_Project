using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using Windows.Kinect;


public class MeasureDepth : MonoBehaviour
{
    public MultiSourceManager mMultiSource;
    public Texture2D mDepthTexture;

    // Cutoffs
    [Range(0, 1.0f)]
    public float mDepthSensitivity = 1;

    [Range(-10, 10f)]
    public float mWallDepth = -10;

    [Header("Top and Bottom")]
    [Range(-1, 1f)]
    public float mTopCutOff = 1;
    [Range(-1, 1f)]
    public float mBottomCutOff = -1;

    [Header("Left and Right")]
    [Range(-1, 1f)]
    public float mLeftCutOff = -1;
    [Range(-1, 1f)]
    public float mRightCutOff = 1;

    // Depth
    private ushort[] mDepthData = null;
    private CameraSpacePoint[] mCameraSpacePoints = null;
    private ColorSpacePoint[] mColorSpacePoints = null;
    private List<ValidPoint> mValidPoints = null;
    private List<Vector2> mTriggerPoints = null;


    // Kinect
    private KinectSensor mSensor = null;
    private CoordinateMapper mMapper = null;
    private Camera mCamera = null;


    private readonly Vector2Int mDepthResolution = new Vector2Int(512, 424);
    private Rect mRect;

    private void Awake()
    {
        mSensor = KinectSensor.GetDefault();
        mMapper = mSensor.CoordinateMapper;
        mCamera = Camera.main;

        int arraySize = mDepthResolution.x * mDepthResolution.y;

        mCameraSpacePoints = new CameraSpacePoint[arraySize];
        mColorSpacePoints = new ColorSpacePoint[arraySize];
    }


    private void Update()
    {
        mValidPoints = DepthToColor();

        mTriggerPoints = FilterToTrigger(mValidPoints);

        if(Input.GetKeyDown(KeyCode.Space))
        {
            mValidPoints = DepthToColor();

            mRect = CreateRect(mValidPoints);

            mDepthTexture = CreateTexture(mValidPoints);
        }
    }

    private void OnGUI()
    {
        GUI.Box(mRect, "");

        if (mTriggerPoints == null)
            return;

        foreach(Vector2 point in mTriggerPoints)
        {
            Rect rect = new Rect(point, new Vector2(10, 10));
            GUI.Box(rect, "");
        }
    }

    private List<ValidPoint> DepthToColor()
    {
        // Points to return
        List<ValidPoint> validPoints = new List<ValidPoint>();

        // Get Data
        mDepthData = mMultiSource.GetDepthData();

        // Map
        mMapper.MapDepthFrameToCameraSpace(mDepthData, mCameraSpacePoints);
        mMapper.MapDepthFrameToColorSpace(mDepthData, mColorSpacePoints);


        // Filter
        for (int i = 0; i < mDepthResolution.x; i++)
        {
            for (int j = 0; j < mDepthResolution.y / 8; j++)
            {
                // Sample index
                int sampleIndex = (j * mDepthResolution.x) + i;
                sampleIndex *= 8;

                // Cutoff tests
                if (mCameraSpacePoints[sampleIndex].X < mLeftCutOff)
                    continue;

                if (mCameraSpacePoints[sampleIndex].X > mRightCutOff)
                    continue;

                if (mCameraSpacePoints[sampleIndex].Y > mTopCutOff)
                    continue;

                if (mCameraSpacePoints[sampleIndex].Y < mBottomCutOff)
                    continue;

                // Create point
                ValidPoint newPoint = new ValidPoint(mColorSpacePoints[sampleIndex], mCameraSpacePoints[sampleIndex].Z);

                // Depth test
                if (mCameraSpacePoints[sampleIndex].Z >= mWallDepth)
                    newPoint.mWithinWallDepth = true;

                // Add
                validPoints.Add(newPoint);
            }
        }

        return validPoints;
    }

    private List<Vector2> FilterToTrigger(List<ValidPoint> validPoints)
    {
        List<Vector2> triggerPoints = new List<Vector2>();

        foreach (ValidPoint point in validPoints)
        {
            if (!point.mWithinWallDepth)
            {
                Vector2 screenPoint = ScreenToCamera(new Vector2(point.colorSpace.X, point.colorSpace.Y));

                if (point.z < mWallDepth * mDepthSensitivity)
                {
                    triggerPoints.Add(screenPoint);
                }

            }
        }

        return triggerPoints;
    }


    private Texture2D CreateTexture(List<ValidPoint> validPoints)
    {
        Texture2D newTexture = new Texture2D(1920, 1080, TextureFormat.Alpha8, false);

        for (int x = 0; x < 1920; x++)
        {
            for (int y = 0; y < 1080; y++)
            {
                newTexture.SetPixel(x, y, Color.clear);
            }
        }

        foreach(ValidPoint point in validPoints)
        {
            newTexture.SetPixel((int)point.colorSpace.X, (int)point.colorSpace.Y, Color.black);
        }

        newTexture.Apply();

        return newTexture;
    }

    #region Rect Creation
    private Rect CreateRect(List<ValidPoint> points)
    {
        if (points.Count == 0)
            return new Rect();

        // Get corners of rect
        Vector2 topLeft = GetTopLeft(points);
        Vector2 bottmRight = GetBottomRigth(points);

        // Translate to viewport
        Vector2 screenTopLeft = ScreenToCamera(topLeft);
        Vector2 screenBottomRight = ScreenToCamera(bottmRight);

        // Rect dimesions
        int width = (int)(screenBottomRight.x - screenTopLeft.x);
        int height = (int)(screenBottomRight.y - screenTopLeft.y);

        // Create
        Vector2 size = new Vector2(width, height);
        Rect rect = new Rect(screenTopLeft, size);

        return rect;
    }

    private Vector2 GetTopLeft(List<ValidPoint> points)
    {
        Vector2 topLeft = new Vector2(int.MaxValue, int.MaxValue);

        foreach(ValidPoint point in points)
        {
            // Left Mmot x
            if (point.colorSpace.X < topLeft.x)
                topLeft.x = point.colorSpace.X;

            // Top most y
            if (point.colorSpace.Y < topLeft.y)
                topLeft.y = point.colorSpace.Y;
        }

        return topLeft;
    }

    private Vector2 GetBottomRigth(List<ValidPoint> points)
    {
        Vector2 bottomRight = new Vector2(int.MinValue, int.MinValue);

        foreach (ValidPoint point in points)
        {
            // Right Mmot x
            if (point.colorSpace.X > bottomRight.x)
                bottomRight.x = point.colorSpace.X;

            // Top most y
            if (point.colorSpace.Y > bottomRight.y)
                bottomRight.y = point.colorSpace.Y;
        }

        return bottomRight;
    }

    private Vector2 ScreenToCamera(Vector2 screenPosition)
    {
        Vector2 normalizdScreen = new Vector2(Mathf.InverseLerp(0, 1920, screenPosition.x), Mathf.InverseLerp(0, 1080, screenPosition.y));

        Vector2 screenPoint = new Vector2(normalizdScreen.x * Camera.main.pixelWidth, normalizdScreen.y * mCamera.pixelHeight);

        return screenPosition;
    }

    #endregion
}


public class ValidPoint
{
    public ColorSpacePoint colorSpace;
    public float z = 0.0f;

    public bool mWithinWallDepth = false;

    public ValidPoint(ColorSpacePoint newColorSpace, float newZ)
    {
        colorSpace = newColorSpace;
        z = newZ;
    }
}