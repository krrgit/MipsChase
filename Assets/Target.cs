using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
   {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
   };

    // External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 6.5f;
    public float m_fScaredDistance = 3.0f;
    public int m_nMaxMoveAttempts = 50;

    // Internal variables.
    public eState m_nState;
    public float m_fHopStart;
    public Vector3 m_vHopStartPos;
    public Vector3 m_vHopEndPos;
    [SerializeField] private Vector2 m_screenBounds;
    [SerializeField] private float m_size;

    void Start()
    {
        // Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindObjectOfType(typeof(Player)) as Player;
        
        m_screenBounds = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
    }

    void StateActionCheck()
    {
        if (m_nState == eState.kIdle)
        {
            if (PlayerInScaredDistance())
            {
                InitHop();
            }
        } else if (m_nState == eState.kHopStart)
        {
            FindEscapePosition();
        }
        else if (m_nState == eState.kHop)
        {
            AnimateHop();
        }
    }

    void InitHop()
    {
        m_fHopStart = Time.time;
        m_nState = eState.kHopStart;
    }

    void FindEscapePosition()
    {
        int attemptCounter = 0;
        m_vHopStartPos = transform.position;
        Vector3 awayDir = (transform.position - m_player.transform.position).normalized;
        Vector3 dir = (new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) + awayDir).normalized;
        m_vHopEndPos = m_vHopStartPos + (m_fHopTime * m_fHopSpeed * dir);
        
        // Find position within screen bounds.
        while (Mathf.Abs(m_vHopEndPos.x) >= m_screenBounds.x || Mathf.Abs(m_vHopEndPos.y) >= m_screenBounds.y)
        {
            dir =  (new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) + awayDir).normalized;
            m_vHopEndPos = m_vHopStartPos + (m_fHopTime * m_fHopSpeed * dir);
            attemptCounter++;

            // Stop: Reached max attempts.
            if (attemptCounter >= m_nMaxMoveAttempts)
            {
                break;
            }
        }
        
        // Max Attempts: Set direction towards player but to the side.
        if (attemptCounter >= m_nMaxMoveAttempts)
        {
            dir = ((Random.Range(0,2) == 0 ? 1 : -1) * Random.Range(0.75f,1f) * Vector3.Cross(-awayDir, -Vector3.forward)) - awayDir;
            m_vHopEndPos = m_vHopStartPos + dir;
        }
        
        // Reset State
        m_nState = eState.kHop;
    }

    void AnimateHop()
    {   
        // Animate
        if (Time.time < m_fHopStart + m_fHopTime)
        {
            transform.position += m_fHopSpeed * Time.fixedDeltaTime * (m_vHopEndPos-m_vHopStartPos).normalized;
        }
        else
        {
            // Stop Animation
            m_nState = eState.kIdle;
            print("Stop anim");
        }
    }

    bool PlayerInScaredDistance()
    {
        float dist = Vector2.Distance(transform.position, m_player.transform.position);
        return dist <= m_fScaredDistance;
    }

    void FixedUpdate()
    {
        StateActionCheck();
        
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving())
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            }
        }
    }
}