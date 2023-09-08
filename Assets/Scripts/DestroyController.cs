using UnityEngine;

public class DestroyController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < -50f)
        {
            Destroy(gameObject);
        }
    }
}
