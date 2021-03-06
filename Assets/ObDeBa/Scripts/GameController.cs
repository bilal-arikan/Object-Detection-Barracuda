using System;
using System.Collections;
using System.Linq;
using TFClassify;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance;


    public bool camAvailable;
    [NonSerialized] public WebCamTexture WebCamCamera;
    private Texture exampleTexture;
    public Classifier classifier;

    private bool isWorking = false;
    private Quaternion baseRotation;

    private void Awake()
    {
        Instance = this;
        Debug.Log(string.Join("\n", WebCamTexture.devices.Select(d =>
        {
            return "> " + d.name + " " + d.kind + " " + string.Join(":", (d.availableResolutions ?? new Resolution[0]).Select(r => r.width + "_" + r.height + "_" + r.refreshRate)) + " " + d.depthCameraName;
        })));

        this.WebCamCamera = new WebCamTexture();
        this.WebCamCamera.Play();
        camAvailable = true;
    }

    private void Update()
    {
        if (!this.camAvailable)
        {
            return;
        }


        if (exampleTexture)
            TFClassify(exampleTexture);
        else
            TFClassify(WebCamCamera);
    }

    private void TFClassify(Texture texture)
    {
        if (this.isWorking)
        {
            return;
        }

        this.isWorking = true;
        StartCoroutine(ProcessImage(texture, classifier.IMAGE_SIZE, result =>
        {
            // if (texture is WebCamTexture)
            // {
            //     var texture2D = new Texture2D(texture.width, texture.height);
            //     texture2D.SetPixels32((texture as WebCamTexture).GetPixels32());
            //     texture2D.Apply(false, false);
            //     texture = texture2D;
            // }

            var forModel = result; // texture;
            StartCoroutine(this.classifier.Classify(forModel, probabilities =>
            {
                UIController.Instance.ShowResult(probabilities);

                Resources.UnloadUnusedAssets();
                this.isWorking = false;
            }));
        }
        ));
    }

    private IEnumerator ProcessImage(Texture texture, int inputSize, System.Action<Color32[]> callback)
    {
        if (texture is WebCamTexture)
            yield return StartCoroutine(TextureTools.CropSquare(texture as WebCamTexture,
                TextureTools.RectOptions.Center, snap =>
                {
                    var scaled = TextureTools.scaled(snap, inputSize, inputSize);
                    var rotated = TextureTools.RotateImageMatrix(scaled.GetPixels32(), scaled.width, scaled.height, 90);
                    callback(rotated);
                }));
        else if (texture is Texture2D)
            yield return StartCoroutine(TextureTools.CropSquare(texture as Texture2D,
                TextureTools.RectOptions.Center, snap =>
                {
                    var scaled = TextureTools.scaled(snap, inputSize, inputSize);
                    var rotated = TextureTools.RotateImageMatrix(scaled.GetPixels32(), scaled.width, scaled.height, 90);
                    callback(rotated);
                }));
    }
}