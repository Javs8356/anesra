using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;

public class QrCodeRecenter : MonoBehaviour
{
    [SerializeField]
    private ARSession session;
    [SerializeField]
    private ARSessionOrigin sessionOrigin;
    [SerializeField]
    private ARCameraManager cameraManager;
    [SerializeField]
    private TargetHandler targetHandler;
    [SerializeField]
    private GameObject qrCodeScanningPanel;

    private Texture2D cameraImageTexture;
    private IBarcodeReader reader = new BarcodeReader(); //crea una instancia del lector
    private bool scanningEnabled = false;

    private void OnEnable()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if(!scanningEnabled)
        {
            return;
        }

        if(!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            return;
        }

        var conversionParams = new XRCpuImage.ConversionParams {
            //Obtiene la imagen entera 
            inputRect = new RectInt(0, 0, image.width, image.height),
            //Mitad de resolución
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
            //Escoge el formato RGBA
            outputFormat = TextureFormat.RGBA32,
            //Gira a lo largo del eje vertical (iamegn espejo)
            transformation = XRCpuImage.Transformation.MirrorY
        };

        //Mira cuantos bytes se necesitan para almacenar la imagen final
        int size = image.GetConvertedDataSize(conversionParams);

        //Asigna un buffer para almacenar la imagen
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        //Extrae los datos de la imagen
        image.Convert(conversionParams, buffer);

        //La imagen fue convertida al formato RGBA32 e insertada en el buffer para que se pueda disponer de la XRCpuImage. Esto debe hacerse, de lo contrario se filtraran recursos.
        image.Dispose();

        //Ya tenemos los datos. Vamos a insertarlos en una textura para poder visualizarlos
        cameraImageTexture = new Texture2D(conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, conversionParams.outputFormat, false);

        cameraImageTexture.LoadRawTextureData(buffer);
        cameraImageTexture.Apply();

        //Los datos temporales ya no son necesarios, por lo que los eliminamos
        buffer.Dispose();

        //Detecta y decodifica el código dentro del bitmap
        var result =  reader.Decode(cameraImageTexture.GetPixels32(), cameraImageTexture.width, cameraImageTexture.height);

        if(result != null)
        {
            SetQrCodeRecenterTarget(result.Text);
            ToggleScanning();
        }
    }

    private  void SetQrCodeRecenterTarget(string targetText)
    {
        TargetFacade currentTarget = targetHandler.GetCurrentTargetByTargetText(targetText);
        if(currentTarget != null)
        {
            //Reseta la posición y la rotación de la ARSession
            session.Reset();

            //Añade un offset al recentering
            sessionOrigin.transform.position = currentTarget.transform.position;
            sessionOrigin.transform.rotation = currentTarget.transform.rotation;
        }
    }

    public void ChangeActiveFloor(string floorEntrance)
    {
        SetQrCodeRecenterTarget(floorEntrance);
    }

    public void ToggleScanning()
    {
        scanningEnabled = !scanningEnabled;
        qrCodeScanningPanel.SetActive(scanningEnabled);
    }
}
