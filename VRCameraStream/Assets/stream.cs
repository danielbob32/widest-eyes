
using UnityEngine;
using UnityEngine.UI;
public class stream : MonoBehaviour
{
    [SerializeField] private RawImage img = default;

    private WebCamTexture webCam;
    // Start is called before the first frame update
    void Start()
    {
        webCam = new WebCamTexture();
        if(!webCam.isPlaying) webCam.Play();
        img.texture = webCam;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
