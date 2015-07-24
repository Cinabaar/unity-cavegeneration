using UnityEngine;

namespace Assets.Scripts
{
    public class Player : MonoBehaviour
    {
        private Rigidbody rigidbody;
        private Vector3 velocity;
        // Use this for initialization
        void Start ()
        {
            rigidbody = GetComponent<Rigidbody>();

        }
	
        // Update is called once per frame
        void Update () {
	        velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized *10;
        }

        void FixedUpdate()
        {
            rigidbody.MovePosition(rigidbody.position + velocity * Time.fixedDeltaTime);
        }
    }
}
