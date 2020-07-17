using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageViewer : MonoBehaviour
{
    public MultiSourceManager mMultiSource;
    public MeasureDepth mMeasureDepth;

    public RawImage mRawImage; 
    public RawImage mRawDepth;

    void Update()
    {
        mRawImage.texture = mMultiSource.GetColorTexture();


        mRawDepth.texture = mMeasureDepth.mDepthTexture;
    }
}
