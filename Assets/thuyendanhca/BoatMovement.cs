using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public float speed = 5f;
    public float circleRadius = 20f;
    public float bobAmplitude = 0.5f;
    public float bobFrequency = 1f;

    private float initialY;
    private float angle = 0f;
    private Vector3 centerPosition;

    void Start()
    {
        initialY = transform.position.y;
        // Assume the boat starts on the edge of the circle
        centerPosition = transform.position - transform.forward * circleRadius;
    }

    void Update()
    {
        // Calculate new angle based on speed and radius
        angle += (speed / circleRadius) * Time.deltaTime;
        
        float x = Mathf.Sin(angle) * circleRadius;
        float z = Mathf.Cos(angle) * circleRadius;
        
        Vector3 newPos = centerPosition + new Vector3(x, 0, z);
        
        // Add bobbing effect
        float newY = initialY + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        newPos.y = newY;
        
        // Calculate rotation to face movement direction
        Vector3 direction = newPos - transform.position;
        direction.y = 0; // Keep rotation horizontal
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }

        transform.position = newPos;
    }
}