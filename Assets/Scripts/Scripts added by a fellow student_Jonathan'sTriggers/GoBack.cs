using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GoBack : MonoBehaviour
{
    
    [Header("Audio")]
    public int stingerID = 16; 


        void OnTriggerEnter(){
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStinger(stingerID);
            }
        }
    }