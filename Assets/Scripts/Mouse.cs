using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class Mouse : MonoBehaviour
{
    public float upSpeed;
    int score = 0;
    AudioSource audio;
    public TextMeshProUGUI scoreText;


    private void Awake()
    {
        audio = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position.y > 6f )
        {
            SceneManager.LoadScene("Game");
        }
    }

    private void FixedUpdate()
    {
        transform.Translate(0, upSpeed, 0);
    }

    private void OnMouseDown()
    {
        score++;
        audio.Play();
        ResetPosition();
        scoreText.text = score.ToString();
    }
    void ResetPosition()
    {
        float randomX = Random.Range(-2.5f, 2.5f);
        transform.position = new Vector2(randomX, randomX - 6f);

    }


}
