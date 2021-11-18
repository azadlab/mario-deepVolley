using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    Blue = 0,
    Red = 1,
    Default = 2
}

public enum Event
{
    HitRedGoal = 0,
    HitBlueGoal = 1,
    HitOutOfBounds = 2,
    HitIntoBlueArea = 3,
    HitIntoRedArea = 4
}

public class VolleyballEnvController : MonoBehaviour
{
    int ballSpawnSide;

    VolleyballSettings volleyballSettings;

    public VolleyballAgent blueAgent;
    public VolleyballAgent redAgent;

    public List<VolleyballAgent> AgentsList = new List<VolleyballAgent>();
    List<Renderer> RenderersList = new List<Renderer>();

    Rigidbody blueAgentRb;
    Rigidbody redAgentRb;

    public GameObject ball;
    Rigidbody ballRb;

    public GameObject blueGoal;
    public GameObject redGoal;

    Renderer blueGoalRenderer;

    Renderer redGoalRenderer;

    Team lastHitter;

    private int resetTimer;
    public int MaxEnvironmentSteps;
    
    public int selfVolleyCount = 0;
    public const int MaxSelfVolley = 3; //How long to allow self volley (waiting for better shot)
    public const float  selfVolleyIncentive= 0.1f; // How often should you allow self volley

    void Start()
    {

        
        // Used to control agent & ball starting positions
        blueAgentRb = blueAgent.GetComponent<Rigidbody>();
        redAgentRb = redAgent.GetComponent<Rigidbody>();
        ballRb = ball.GetComponent<Rigidbody>();

        // Starting ball spawn side
        // -1 = spawn blue side, 1 = spawn red side
        var spawnSideList = new List<int> { -1, 1 };
        ballSpawnSide = spawnSideList[Random.Range(0, 2)];

        // Render ground to visualise which agent scored
        blueGoalRenderer = blueGoal.GetComponent<Renderer>();
        redGoalRenderer = redGoal.GetComponent<Renderer>();
        RenderersList.Add(blueGoalRenderer);
        RenderersList.Add(redGoalRenderer);

        volleyballSettings = FindObjectOfType<VolleyballSettings>();
        

        ResetScene();
    }

    /// <summary>
    /// Tracks which agent last had control of the ball
    /// </summary>
    public void UpdateLastHitter(Team team)
    {
        // If it is the same hitter, give a diminshing reward each time it hits
        // Balancing between keeping the ball in play vs scoring a goal
        if(team == lastHitter)
        {
            switch(team)
            {
                case Team.Red:
                    redAgent.AddReward(-1f*selfVolleyIncentive*(float)System.Math.Tanh(selfVolleyCount-MaxSelfVolley));
                    selfVolleyCount++;
                break;
                case Team.Blue:
                    blueAgent.AddReward(-1f*selfVolleyIncentive*(float)System.Math.Tanh(selfVolleyCount-MaxSelfVolley));
                    selfVolleyCount++;
                break;
            }
                
        }
        else 
        {
            // If the goal has been defended, reward for it.
            switch(team)
            {
                case Team.Red:
                    redAgent.AddReward(0.5f);
                    selfVolleyCount = 0;
                break;
                case Team.Blue:
                    blueAgent.AddReward(0.5f);
                    selfVolleyCount = 0;
                break;
            }
        }
        lastHitter = team;
    }

    /// <summary>
    /// Resolves scenarios when ball enters a trigger and assigns rewards
    /// </summary>
    public void ResolveEvent(Event triggerEvent)
    {
        switch (triggerEvent)
        {
            case Event.HitOutOfBounds:
                if (lastHitter == Team.Blue)
                {
                    blueAgent.SetReward(-1f);
                }
                else if (lastHitter == Team.Red)
                {
                    redAgent.SetReward(-1f);
                }

                // end episode
                blueAgent.EndEpisode();
                redAgent.EndEpisode();
                ResetScene();
            break;

            case Event.HitBlueGoal:
                // blue wins
                blueAgent.AddReward(1f);
                redAgent.SetReward(-1f);
                // turn floor blue
                StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.blueGoalMaterial, RenderersList, .5f));

                // end episode
                blueAgent.EndEpisode();
                redAgent.EndEpisode();
                ResetScene();
            break;

            case Event.HitRedGoal:
                // red wins
                redAgent.AddReward(1f);
                blueAgent.SetReward(-1f);
                // turn floor red
                StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.redGoalMaterial, RenderersList, .5f));

                // end episode
                blueAgent.EndEpisode();
                redAgent.EndEpisode();
                ResetScene();
            break;

            case Event.HitIntoBlueArea:
                if (lastHitter == Team.Red)
                {
                    redAgent.AddReward(0.5f);
                }
                                    
            break;

            case Event.HitIntoRedArea:
                if (lastHitter == Team.Blue)
                {
                    blueAgent.AddReward(0.5f);
                }
            break;
        }
    }

    /// <summary>
    /// Changes the color of the ground for a moment.
    /// </summary>
    /// <returns>The Enumerator to be used in a Coroutine.</returns>
    /// <param name="mat">The material to be swapped.</param>
    /// <param name="time">The time the material will remain.</param>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, List<Renderer> rendererList, float time)
    {
        foreach (var renderer in rendererList)
        {
            renderer.material = mat;
        }

        yield return new WaitForSeconds(time); // wait for 2 sec
        
        foreach (var renderer in rendererList)
        {
            renderer.material = volleyballSettings.defaultMaterial;
        }

    }

    /// <summary>
    /// Called every step. Control max env steps.
    /// </summary>
    void FixedUpdate()
    {
        resetTimer+=1;
        if(resetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            blueAgent.EpisodeInterrupted();
            redAgent.EpisodeInterrupted();
            ResetScene();
        }

        if(Input.GetKeyDown("escape"))
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// Reset agent and ball spawn conditions.
    /// </summary>
    public void ResetScene()
    {
        resetTimer = 0;
        lastHitter = Team.Default;

        foreach(var agent in AgentsList)
        {
            var randomPosX = Random.Range(-2f,2f);
            var randomPosZ = Random.Range(-2f,2f);
            var randomPosY = Random.Range(0.5f,3.75f);
            var randomRot = Random.Range(-45f,45f);

            agent.transform.localPosition = new Vector3(randomPosX,randomPosY,randomPosZ);
            agent.transform.eulerAngles  = new Vector3(0,randomRot,0);
        }
        ResetBall();
        selfVolleyCount = 0;
    }

    /// <summary>
    /// Reset ball spawn conditions
    /// </summary>
    void ResetBall()
    {
        var randomPosX = Random.Range(-2f, 2f);
    var randomPosZ = Random.Range(6f, 10f);
    var randomPosY = Random.Range(6f, 8f);

    // alternate ball spawn side
    // -1 = spawn blue side, 1 = spawn red side
    ballSpawnSide = -1 * ballSpawnSide;

    if (ballSpawnSide == -1)
    {
        ball.transform.localPosition = new Vector3(randomPosX, randomPosY, randomPosZ);
    }
    else if (ballSpawnSide == 1)
    {
        ball.transform.localPosition = new Vector3(randomPosX, randomPosY, -1 * randomPosZ);
    }

    ballRb.angularVelocity = Vector3.zero;
    ballRb.velocity = Vector3.zero;
    }
}
