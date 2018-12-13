using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
///     Pather Class:
///     - Holds reference for the path and determines the ultimate force after implementing the seek for the current path index
/// 
/// </summary>

public class Pather : Vehicle {

    // ultimate force vector
    public Vector3 ultimateForce;

    // object references
    public GameObject[] obstacles;
    public GameObject[] path;

    public int currentPathIndex = 0;

    // Update is called once per frame
    private new void Update()
    {
        base.Update();
    }

    /// <summary>
    /// Overwriting calcsteeringforces to control forces
    /// </summary>
    public override void CalcSteeringForces()
    {
        // creating ultimate force vector
        ultimateForce = Vector3.zero;

        // seek current path index
        ultimateForce += Seek(path[currentPathIndex].transform.position);

        // if too close to current index, seek next one
        if (CircleCollision(path[currentPathIndex], gameObject, 2))
        {
            // sets the next target
            currentPathIndex++;

            // prevents out of index error
            if (currentPathIndex >= path.Length)
            {
                currentPathIndex = 0;
            }
        }

        // scale ultimate force to max speed
        ultimateForce = Vector3.ClampMagnitude(ultimateForce, maxSpeed);
        ultimateForce.y = 0;

        // apply ultimate force
        ApplyForce(ultimateForce);
    }
}
