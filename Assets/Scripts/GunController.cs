using UnityEngine;

public class GunController : MonoBehaviour
{
    public Object bulletPrefab;
    public float bulletSpeed = 10f;
    public float bulletLife = 2f;

    private float _offset;
    
    // Start is called before the first frame update
    void Start()
    {
        _offset = transform.localScale.z / 2;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        var pos = transform.position + transform.forward * _offset;
        var bullet = Instantiate(bulletPrefab, pos, transform.rotation) as GameObject;
        bullet.GetComponent<Rigidbody>().velocity = transform.forward * bulletSpeed;
        Destroy(bullet, bulletLife);
    }
}
