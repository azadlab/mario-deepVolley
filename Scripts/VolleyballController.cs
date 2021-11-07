using UnityEngine;

public class VolleyballController : MonoBehaviour
{
    public VolleyballEnvController envController;
    public GameObject redGoal;
    public GameObject blueGoal;
    Collider redGoalCollider;
    Collider blueGoalCollider;
    void Start()
    {
        redGoalCollider = redGoal.GetComponent<Collider>();
        blueGoalCollider = blueGoal.GetComponent<Collider>();

        envController = GetComponentInParent<VolleyballEnvController>();

    }

    /// <summary>
    /// Detects whether the ball lands in the blue, red, or out of bounds area
    /// </summary>
    void OnTriggerEnter(Collider other)
    {

        if(other.gameObject.CompareTag("boundary"))
        {
            envController.ResolveEvent(Event.HitOutOfBounds);
            
        }
        else if(other.gameObject.CompareTag("blueBoundary"))
        {
            envController.ResolveEvent(Event.HitIntoBlueArea);
        }
        else if(other.gameObject.CompareTag("redBoundary"))
        {
            envController.ResolveEvent(Event.HitIntoRedArea);
        }
        else if(other.gameObject.CompareTag("redGoal"))
        {
            envController.ResolveEvent(Event.HitRedGoal);
        }
        else if(other.gameObject.CompareTag("blueGoal"))
        {
            envController.ResolveEvent(Event.HitBlueGoal);
        }
        
    }


}