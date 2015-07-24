using UnityEngine;

namespace Assets.Scripts
{
    public class Player2d : MonoBehaviour
    {
        private Rigidbody2D rigidbody;
        private Vector2 velocity;
        // Use this for initialization
        void Start ()
        {
            rigidbody = GetComponent<Rigidbody2D>();

        }
	
        // Update is called once per frame
        void Update () {
	        velocity = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized *10;
        }

        void FixedUpdate()
        {
            rigidbody.MovePosition(rigidbody.position + velocity * Time.fixedDeltaTime);
        }
    }
}
