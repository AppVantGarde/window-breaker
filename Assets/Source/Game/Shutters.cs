using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shutters : MonoBehaviour
{
    public static List<List<float[]>> shutterTimes;

    public SharedPersistentInt currentLevel;

    public Animator animator;

    private bool _opened;
    private int _bucketIndex;
    private float _nextShutterAnimation = -0.1f;

    public void Awake( )
    {
        if(shutterTimes == null)
        {
            float[ ][ ] baseRanges = new float[5][ ];
            baseRanges[0] = new float[ ] { 2.5f,     4f };   // Levels 1 - 10
            baseRanges[1] = new float[ ] { 1.5f,    2.5f }; // Levels 11 - 20
            baseRanges[2] = new float[ ] { 1.25f,     2f };   // Levels 21 - 30
            baseRanges[3] = new float[ ] { 1f,      1.5f }; // Levels 31 - 40
            baseRanges[4] = new float[ ] { 0.5f,   0.85f }; // Levels 41 - 50

            shutterTimes = new List<List<float[ ]>>( );
            for(int i = 0; i < baseRanges.Length; i++)
            {
                shutterTimes.Add( new List<float[ ]>( ) );
                for(int ii = 0; ii < baseRanges.Length; ii++)
                {
                    float min = 0.5f * baseRanges[i][0] + 0.2f * baseRanges[i][0] * UnityEngine.Random.value;
                    float max = 0.7f * baseRanges[i][1] + 0.35f * baseRanges[i][1] * UnityEngine.Random.value;
                    shutterTimes[i].Add( new float[ ] { min, max } );
                }
            }

            //shutterTimes = new List<float[ ]>( );
            //for(int i = 0; i < 20; i++)
            //{
            //    float min = 0.5f * (0.5f + (i * 0.15f)) + 0.2f * (0.5f + (i * 0.15f)) * UnityEngine.Random.value;
            //    float max = 0.7f * (1.25f + (i * 0.15f)) + 0.35f * (1.25f + (i * 0.15f)) * UnityEngine.Random.value;
            //    shutterTimes.Add( new float[ ] { min, max } );
            //}
        }

        if(currentLevel == null)
            currentLevel = Resources.Load<SharedPersistentInt>( "CurrentLevel" );

        _bucketIndex = Mathf.FloorToInt( currentLevel.Value / 50 );//Random.Range( 0, shutterTimes.Count );
    }

    public void Update( )
    {
        _nextShutterAnimation -= Time.deltaTime;

        if(_nextShutterAnimation <= 0)
        {
            _opened = !_opened;

            animator.Play( _opened ? "close" : "open" );

            int rangeIndex = Random.Range( 0, shutterTimes[_bucketIndex].Count );
            float shutterTime = Random.Range( shutterTimes[_bucketIndex][rangeIndex][0], shutterTimes[_bucketIndex][rangeIndex][1] );

            _nextShutterAnimation += 0.75f * shutterTime + 0.5f * shutterTime * Random.value;
        }
    }
}
