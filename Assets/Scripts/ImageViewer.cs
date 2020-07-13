using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageViewer : MonoBehaviour
{
    public MultiSourceManager mMultiSource; // 깊이 데이터, 칼라데이터를 넣을 변수
    public RawImage mRawImage; // 이미지 넣은 변수

    void Update()
    {
        mRawImage.texture = mMultiSource.GetColorTexture();
    }
}
