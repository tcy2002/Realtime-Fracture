using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    
    public Text text;
    private float _time;
    private int _frameCount;

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }
        _instance = this;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Application.Quit();
        }

        // 显示帧率，每一秒更新一次
        _time += Time.deltaTime;
        _frameCount++;
        if (_time < 0.2)
        {
            return;
        }
        text.text = $"FPS: {_frameCount / _time:F2}";
        _time = 1e-4f;
        _frameCount = 0;
    }
}
