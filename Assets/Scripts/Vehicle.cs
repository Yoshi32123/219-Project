using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Vehicle Class:
///     - Stores reference for all of Craig Reynolds Autonomous Agent Methods
/// </summary>
public abstract class Vehicle : MonoBehaviour
{

    // vectors necessary for autonomous agent movement
    public Vector3 acceleration;
    public Vector3 velocity;
    public Vector3 position;
    public Vector3 direction;

    // floats for force-based movement
    public float mass;
    public float maxSpeed;
    public float safeDistance;
    public float radius;
    public float angle = 10000;
    public float timer = 0;

    public bool obstacleAvoid = false;

    // lists for zombies/humans
    public List<GameObject> humans;
    public List<GameObject> zombies;
    public new Camera camera;

    // Use this for initialization
    public void Start()
    {
        position = transform.position;
        velocity = new Vector3(5, 0, 5);
    }

    // Update is called once per frame
    public void Update()
    {
        // setting the height to the terrain height
        position.y = Terrain.activeTerrain.SampleHeight(position);

        // steering force
        CalcSteeringForces();

        RotateToTarget();

        // forced based movement
        velocity += acceleration * Time.deltaTime;
        position += velocity * Time.deltaTime;
        direction = velocity.normalized;
        acceleration = Vector3.zero;
        transform.position = position;
        transform.forward = direction;
    }

    /// <summary>
    /// Applyies an incoming force, divide by mass, and get the cumulative accel vector
    /// </summary>
    public void ApplyForce(Vector3 force)
    {
        acceleration += force / mass;
    }

    // seek method
    // params: GameObject, vector 3 target location
    public Vector3 Seek(Vector3 targetLocation)
    {
        // Step 1: Calculate desired velocity
        Vector3 desiredVelocity = targetLocation - gameObject.transform.position;

        // Step 2: "Shrink" DV to max speed
        //desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, maxSpeed);

        // Another way of "setting" magnitude of DV
        // Normalize and * maxSpeed
        desiredVelocity.Normalize();
        desiredVelocity = desiredVelocity * maxSpeed;

        // Step 3: Calculate Seek force
        // SF = DV - CV
        Vector3 seekForce = desiredVelocity - velocity;

        // Step 4: Apply seek force
        return seekForce;
    }
    /// <summary>
    /// Overloaded version of Seek
    /// </summary>
    /// <param name="target">passes in a GameObject</param>
    public Vector3 Seek(GameObject target)
    {
        return Seek(target.transform.position);
    }

    /// <summary>
    /// Flee method
    /// </summary>
    /// <param name="targetLocation"></param>
    public Vector3 Flee(Vector3 targetLocation)
    {
        // negative vector of seek calculation
        Vector3 desiredVelocity = gameObject.transform.position - targetLocation;
        desiredVelocity.Normalize();
        desiredVelocity = desiredVelocity * maxSpeed;
        Vector3 fleeForce = desiredVelocity - velocity;
        return fleeForce;
    }
    /// <summary>
    /// Overloaded version of Flee
    /// </summary>
    /// <param name="target">passes in a GameObject</param>
    public Vector3 Flee(GameObject target)
    {
        return Flee(target.transform.position);
    }


    /// <summary>
    /// Rotates towards target
    /// </summary>
    public void RotateToTarget()
    {
        transform.LookAt(position + direction);
    }

    /// <summary>
    /// Abstact method creation
    /// </summary>
    public abstract void CalcSteeringForces();

    /// <summary>
    /// Returns a bool for if approching min/max
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public bool StayOnScreen(int min, int max)
    {
        // checking x position
        if (position.x < min)
        {
            return true;
        }
        else if (position.x > max)
        {
            return true;
        }

        // checking z position
        if (position.z < min)
        {
            return true;
        }
        else if (position.z > max)
        {
            return true;
        }

        // if not close to an edge
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Determines steering for vehicles to avoid running into obstacles
    /// </summary>
    /// <returns></returns>
    protected Vector3 ObstacleAvoidance(GameObject obstacle)
    {
        // Info needed for obstacle avoidance
        Vector3 vecToCenter = obstacle.transform.position - position;
        vecToCenter.y = 0;
        //float dotForward = Vector3.Dot(vecToCenter, direction);
        float dotForward = Vector3.Dot(vecToCenter, transform.forward);
        //float dotRight = Vector3.Dot(vecToCenter, Quaternion.Euler(0, 90, 0) * direction);
        float dotRight = Vector3.Dot(vecToCenter, transform.right);
        float radiiSum = obstacle.GetComponent<Obstacle>().radius + radius;

        // Step 1: Are there objects in front of me?  
        // If obstacle is behind, ignore, no need to steer - exit method
        // Compare dot forward < 0
        if (dotForward < 0)
        {
            obstacleAvoid = false;
            //Debug.DrawLine(transform.position, transform.position + vecToCenter, Color.red);
            return Vector3.zero;
        }

        // Step 2: Are the obstacles close enough to me?  
        // Do they fit within my "safe" distance
        // If the distance > safe, exit method
        if (vecToCenter.magnitude > safeDistance)
        {
            obstacleAvoid = false;
            //Debug.DrawLine(transform.position, transform.position + vecToCenter, Color.yellow);
            return Vector3.zero;
        }

        // Step 3:  Check radii sum against distance on one axis
        // Check dot right, 
        // If dot right is > radii sum, exit method
        if (radiiSum < Mathf.Abs(dotRight))
        {
            // Debug.DrawLine(transform.position, transform.position + vecToCenter, Color.blue);
            return Vector3.zero;
        }

        // NOW WE HAVE TO STEER!  
        // The only way to get to this code is if the obstacle is in my path
        // Determine if obstacle is to my left or right
        // Desired velocity in opposite direction * max speed
        obstacleAvoid = true;

        Vector3 desiredVelocity;

        if (dotRight < 0)        // Left
        {
            desiredVelocity = transform.right * maxSpeed;
        }
        else                    // Right
        {
            desiredVelocity = -transform.right * maxSpeed;
        }

        // Debug line to obstacle
        // Helpful to see which obstacle(s) a vehicle is attempting to maneuver around
        Debug.DrawLine(transform.position, obstacle.transform.position, Color.green);

        // Return steering force
        Vector3 steeringForce = desiredVelocity - velocity;
        return steeringForce;
    }

    /// <summary>
    /// Makes an object wander across the screen as opposed to seeking/fleeing
    /// </summary>
    /// <returns></returns>
    public Vector3 Wander()
    {
        // Step 1: choosing a distance ahead to have a circle at
        Vector3 circleCenter = velocity;
        Vector3 velRef = velocity;
        velRef.Normalize();
        Vector3 circleRadius = Quaternion.Euler(0, -90, 0) * velRef * 2;

        // Step 2: find a random angle and implement it
        if (angle == 10000)
        {
            angle = Random.Range(0, 360);
        }
        else
        {
            angle += Random.Range(-10, 11);
        }

        circleRadius = Quaternion.Euler(0, angle, 0) * circleRadius;

        // Step 3: return the position on the circle to seek;
        Vector3 wanderForce = position + circleCenter + circleRadius;

        return Seek(wanderForce);
    }

    /// <summary>
    /// Makes sure there is no intersection between objects
    /// </summary>
    //public Vector3 Separation(GameObject original, List<GameObject> neighbors)
    //{
    //    // Resultant force
    //    Vector3 finalForce = Vector3.zero;
    //
    //    // 1 - Find "too close" neighbors
    //    List<GameObject> closeNeighbors = new List<GameObject>();
    //    List<GameObject> closerNeighbors = new List<GameObject>();
    //
    //    // Loop through neighbors
    //    for (int i = 0; i < neighbors.Count; i++)
    //    {
    //        // Make sure the original isn't equal to the iteration of the neighbor
    //        if (original != neighbors[i])
    //        {
    //            // If there is a collision
    //            if (CircleCollision(neighbors[i], original, 2))
    //            {
    //                // If there is a smaller collision
    //                if (CircleCollision(neighbors[i], original, 1))
    //                {
    //                    // Add it to closer neighbors
    //                    closerNeighbors.Add(neighbors[i]);
    //                }
    //
    //                // If there isn't a smaller collision
    //                else
    //                {
    //                    // Add it to close neighbors
    //                    closeNeighbors.Add(neighbors[i]);
    //                }
    //            }
    //        }
    //    }
    //
    //    // 2 - Calculate a steering vector away from each neighbor
    //    // If there are objects in closeNeighbors
    //    if (closeNeighbors.Count > 0)
    //    {
    //        // Loop though and call obstacle avoidance and add the resultant force to the final force
    //        foreach (GameObject neighbor in closeNeighbors)
    //        {
    //            //Debug.DrawLine(transform.position, neighbor.transform.position, Color.green);
    //            finalForce += original.GetComponent<Vehicle>().ObstacleAvoidance(neighbor);
    //        }
    //    }
    //
    //    // If there are objects in closerNeighbors
    //    if (closerNeighbors.Count > 0)
    //    {
    //        // Loop though and call obstacle avoidance and add the resultant force to the final force
    //        foreach (GameObject neighbor in closerNeighbors)
    //        {
    //            Debug.DrawLine(transform.position, neighbor.transform.position, Color.green);
    //            finalForce += original.GetComponent<Vehicle>().ObstacleAvoidance(neighbor) * 2;
    //        }
    //    }
    //
    //    // 3 - Use weights that are inversely proportional to distance (1/dist)
    //    // (Used in calc forces method)
    //
    //    // 4 - Sum all(done above), return final steering force
    //    return finalForce;
    //}

    /// <summary>
    /// Pursue method passes predicted position into Seek
    /// </summary>
    /// <param name="target">passes in a GameObject</param>
    public Vector3 Pursue(GameObject target)
    {
        // adding predicted position
        Vector3 targetLocation = target.transform.position + target.GetComponent<Vehicle>().velocity / 2;

        // call seek method
        return Seek(targetLocation);
    }

    /// <summary>
    /// Evade method passes predicted position into Flee
    /// </summary>
    /// <param name="target">passes in a GameObject</param>
    public Vector3 Evade(GameObject target)
    {
        // adding predicted position
        Vector3 targetLocation = target.transform.position + target.GetComponent<Vehicle>().velocity / 2;

        // call seek method
        return Flee(targetLocation);
    }

    /// <summary>
    /// Checks for collision
    /// </summary>
    /// <returns></returns>
    public bool CircleCollision(GameObject obj1, GameObject obj2, int extraLength)
    {
        // get vector between circle centers
        Vector3 vectorBetween = obj1.transform.position - obj2.transform.position;
        float lengthBetween = Mathf.Pow(vectorBetween.x, 2) + Mathf.Pow(vectorBetween.z, 2);

        // setting radii from each circle
        float rad1 = obj1.GetComponent<Obstacle>().radius + extraLength;
        float rad2 = obj2.GetComponent<Obstacle>().radius;

        // check if distance between centers is less than the radii combined
        if (lengthBetween < Mathf.Pow((rad1 + rad2), 2))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Makes sure all are going the same general direction
    /// </summary>
    public Vector3 Alignment(GameObject original, List<GameObject> neighbors)
    {
        // fields to help
        Vector3 desiredVelocity;
        Vector3 averageDirection = Vector3.zero;
        Vector3 neighborVelocity = Vector3.zero;

        // computing desiredVelocity: normalizing the sum and multiplying by maxSpeed
        averageDirection.Normalize();
        desiredVelocity = averageDirection * original.GetComponent<Vehicle>().maxSpeed;

        // calculate steering force
        desiredVelocity = desiredVelocity - original.GetComponent<Vehicle>().velocity;

        return desiredVelocity;
    }

    /// <summary>
    /// Coheres the "original" to the center of the flock
    /// </summary>
    /// <param name="original"></param>
    /// <param name="neighbors"></param>
    /// <returns></returns>
    public Vector3 Cohesion(GameObject original, List<GameObject> neighbors)
    {
        // Helper fields
        Vector3 centroidPosition;
        float members = 0;
        Vector3 sumOfPositions = Vector3.zero;

        // get flocks average position
        for (int i = 0; i < neighbors.Count; i++)
        {
            sumOfPositions += neighbors[i].transform.position;
            members++;
        }

        // disregards y position
        sumOfPositions.y = 0;

        // calculating centroid
        centroidPosition = sumOfPositions / members;


        return Seek(centroidPosition);
    }
}
