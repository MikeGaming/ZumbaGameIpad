using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ZumbaController : MonoBehaviour {

  //public GameObject VolumetricObject;

  public List<GameObject> songStanding;
  public List<GameObject> songSitting;
  [Header("FIND ME INT DETERMINES THE SONG AT WHIHC FIND ME HAPPENS (index starts at 0)")]
  public List<int> nextScene;
  public int findMeScene;

  bool songStarted;
  public GameObject currentSong;
public ZumbaFeedback feedback;
    bool feedbackTriggered = false;

    public float timer;
  public TextMeshProUGUI text;
  public bool isFinished;
  public float songTimer = 3;
  public TextMeshProUGUI score;
  public TextMeshProUGUI results;

  void Awake()
    {
       
    }
  // Start is called before the first frame update
  void Start() {
    songTimer = 3;
    isFinished = false;
    //VolumetricObject = GameObject.Find("VolumetricSDK");
    feedback = FindObjectOfType<ZumbaFeedback>();
        feedbackTriggered = false;
    }

  // Update is called once per frame
  void Update() {
    if (timer <= 0) {
      text.text = "";
      if (!songStarted) {
        StartSong();
        feedback.smallStarFillDuration = currentSong.GetComponent<AudioSource>().clip.length;
                feedback.StartFillingNextStar();
            }
      if ((!currentSong.GetComponent<AudioSource>().isPlaying || Input.GetKeyDown(KeyCode.S)) && currentSong.GetComponent<CustomVideoPlayer>().songStarted) {//if song audio is finished
        isFinished = true;
        if (!feedbackTriggered)
                {
                    feedbackTriggered = true;
                    feedback.TriggerStar(true);
                }
      }
      if (isFinished)
            {
                songTimer -= Time.deltaTime;//start a 3 second countdown delay
                if (songTimer <= 0)
                {//when delay is finished
                    Menu.song++;//boot next song for next load
                                //if (Menu.song == findMeScene) {
                    int rand = Random.Range(6, 11);
                    //Debug.Log("memememememememeemememememememe:" + rand);
                    nextScene[4] = rand;
                    //SceneManager.LoadScene(rand);
                    //}
                    SceneManager.LoadScene(nextScene[Menu.song]);//send to next scene

                }
            }
      

      
    } else if (timer > 0) {
      timer -= Time.deltaTime;
      text.text = timer.ToString();
    }
  }
  void StartSong() {
        //VolumetricObject.SetActive(true);
        if (Menu.isStanding) {//if standing based
      songStanding[Menu.song].SetActive(true);//enable song
      currentSong = songStanding[Menu.song];
    } else {//sitting
      songSitting[Menu.song].SetActive(true);//enable song
      currentSong = songSitting[Menu.song];
    }
    songStarted = true;
  }
}
