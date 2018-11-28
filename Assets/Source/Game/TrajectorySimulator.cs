using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectorySimulator
{
    //private float _drag = 0;
    //private Vector3 _velocity;
    //private Vector3 _position;
    //private Vector3 _gravity;

    //public List<Vector3> trajectoryPath = new List<Vector3>( );

    public float accuracy = 0.98f;
    public int iterationLimit = 150;
    public LayerMask raycastMask = -1;
    public bool checkForCollision = true;
    private static bool bounceOnCollision = false;
    public List<Vector3> predictionPoints = new List<Vector3>( );
    public RaycastHit lastHit;

    private float drag;
    private Vector3 vel;
    private Vector3 pos;
    private Vector3 grav;

    public void RunSimulation( Vector3 startPosition, Vector3 velocity, Vector3 gravity, float linearDrag = 0f )
    {
        drag = linearDrag;
        vel = velocity;
        pos = startPosition;
        grav = gravity;

        PerformPrediction( );

        //_gravity = gravity;
        //_velocity = velocity;
        //_position = startPosition;

        //Vector3 direction = Vector3.zero;
        //Vector3 toPosition;

        //trajectoryPath.Clear( );

        //float lineDistance = 0;
        //float compAcc = 1f - 0.98f;
        //Vector3 gravAdd = _gravity * compAcc;
        //float dragMult = Mathf.Clamp01( 1f - _drag * compAcc );
        //bool complete = false;
        //int simulationStep = 0;
        //int maxSimulationStep = 150;
        //while(!complete && simulationStep < maxSimulationStep)
        //{
        //    _velocity += gravAdd;
        //    _velocity *= dragMult;
        //    toPosition = _position + velocity * compAcc;
        //    direction = toPosition - _position;
        //    trajectoryPath.Add( _position );

        //    float distance = Vector3.Distance( _position, toPosition );
        //    lineDistance += distance;

        //    Ray rayCast = new Ray( _position, direction );
        //    RaycastHit hit;
        //    if(Physics.Raycast( rayCast, out hit, distance, -1 ))
        //    {
        //        trajectoryPath.Add( hit.point );
        //        complete = true;
        //    }

        //    _position = toPosition;
        //    simulationStep++;
        //}

        //for(int i = 1; i < trajectoryPath.Count; i++)
        //{
        //    Debug.DrawLine( trajectoryPath[i - 1], trajectoryPath[i], Color.blue );
        //}
    }

    float lineDistance = 0f;
    private void PerformPrediction( )
    {
        Vector3 dir = Vector3.zero;
        Vector3 toPos;
        bool done = false;
        int iter = 0;
        lineDistance = 0f;

        float compAcc = 1f - accuracy;
        Vector3 gravAdd = grav * compAcc;
        float dragMult = Mathf.Clamp01( 1f - drag * compAcc );
        predictionPoints.Clear( );
        while(!done && iter < iterationLimit)
        {
            vel += gravAdd;
            vel *= dragMult;
            toPos = pos + vel * compAcc;
            dir = toPos - pos;
            predictionPoints.Add( pos );

            float dist = Vector3.Distance( pos, toPos );
            lineDistance += dist;
            if(checkForCollision)
            {
                Ray ray = new Ray( pos, dir );
                RaycastHit hit;
                if(Physics.Raycast( ray, out hit, dist, raycastMask ))
                {
                    lastHit = hit;
                    predictionPoints.Add( hit.point );
                    done = true;
                    //if(bounceOnCollision)
                    //{
                    //    //Collider col = rb.GetComponent<Collider>( );
                    //    //if(col)
                    //    //{
                    //    //    PhysicMaterial pMat = col.sharedMaterial;
                    //    //    if(pMat)
                    //    //    {

                    //    //        toPos = hit.point;
                    //    //        //Debug.Log();
                    //    //        vel = Vector3.Reflect( vel, hit.normal ) * pMat.bounciness;
                    //    //    }
                    //    //}
                    //}
                    //else
                    //{
                    //    done = true;
                    //}
                }
            }

            pos = toPos;
            iter++;
        }

        for(int i = 1; i < predictionPoints.Count; i++)
        {
            Debug.DrawLine( predictionPoints[i - 1], predictionPoints[i], Color.blue );
        }
    }
}
