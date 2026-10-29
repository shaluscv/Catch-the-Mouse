using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// PUBLIC_INTERFACE
/// <summary>Controls catches, the run lifecycle, and locally persisted results.</summary>
public class Mouse : MonoBehaviour
{
    private const string PersonalBestKey = "CatchTheMouse.PersonalBest";
    public float upSpeed;
    public TextMeshProUGUI scoreText;

    private int score;
    private int personalBest;
    private bool gameOver;
    private bool restarting;
    private AudioSource catchAudio;
    private GameObject resultsPanel;
    private TextMeshProUGUI finalScoreText;
    private TextMeshProUGUI bestScoreText;
    private Button replayButton;

    private void Awake()
    {
        catchAudio = GetComponent<AudioSource>();
        personalBest = Mathf.Max(0, PlayerPrefs.GetInt(PersonalBestKey, 0));
    }

    private void Start()
    {
        scoreText.text = "0";
        CreateResultsUI();
    }

    private void Update()
    {
        if (!gameOver && transform.position.y > 6f)
            EndRun();
    }

    private void FixedUpdate()
    {
        if (gameOver)
            return;

        if (transform.position.y > 6f)
        {
            EndRun();
            return;
        }

        // Preserve the existing per-fixed-update movement increment.
        transform.Translate(0, upSpeed, 0);
    }

    private void OnMouseDown()
    {
        if (gameOver || restarting)
            return;

        // Input may arrive before Update observes a missed mouse.
        if (transform.position.y > 6f)
        {
            EndRun();
            return;
        }

        // Also reject UI-originated presses after a replay scene load.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        score++;
        if (catchAudio != null)
            catchAudio.Play();
        ResetPosition();
        scoreText.text = score.ToString();
    }

    private void EndRun()
    {
        if (gameOver)
            return;

        gameOver = true;
        if (score > personalBest)
        {
            personalBest = score;
            PlayerPrefs.SetInt(PersonalBestKey, personalBest);
            PlayerPrefs.Save();
        }

        // State guards remain authoritative even if a physics callback was queued.
        Collider2D mouseCollider = GetComponent<Collider2D>();
        if (mouseCollider != null)
            mouseCollider.enabled = false;

        finalScoreText.text = "Final Score: " + score;
        bestScoreText.text = "Personal Best: " + personalBest;
        resultsPanel.SetActive(true);
        replayButton.Select();
    }

    private void PlayAgain()
    {
        if (!gameOver || restarting)
            return;

        restarting = true;
        replayButton.interactable = false;
        SceneManager.LoadScene("Game");
    }

    private void ResetPosition()
    {
        float randomX = Random.Range(-2.5f, 2.5f);
        transform.position = new Vector2(randomX, randomX - 6f);
    }

    private void CreateResultsUI()
    {
        // Use the scene's existing canvas, scaler, raycaster and event system.
        Canvas canvas = scoreText.GetComponentInParent<Canvas>();
        resultsPanel = new GameObject("GameOverResults", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = resultsPanel.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        SetBounds(panelRect, Vector2.zero, Vector2.one);
        resultsPanel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.97f);

        // Relative bounds keep all controls inside both portrait and landscape views.
        CreateLabel("Title", "Game Over", panelRect, 0.68f, 0.84f, 96f);
        finalScoreText = CreateLabel("FinalScore", "", panelRect, 0.51f, 0.64f, 64f);
        bestScoreText = CreateLabel("PersonalBest", "", panelRect, 0.37f, 0.50f, 64f);

        GameObject buttonObject = new GameObject("PlayAgain", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(panelRect, false);
        SetBounds(buttonRect, new Vector2(0.20f, 0.17f), new Vector2(0.80f, 0.30f));
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = Color.white;
        replayButton = buttonObject.GetComponent<Button>();
        replayButton.targetGraphic = buttonImage;
        ColorBlock colors = replayButton.colors;
        colors.normalColor = new Color(0.95f, 0.78f, 0.25f);
        colors.highlightedColor = new Color(1f, 0.9f, 0.5f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.7f, 0.52f, 0.1f);
        replayButton.colors = colors;
        TextMeshProUGUI buttonLabel = CreateLabel("Label", "Play Again", buttonRect, 0.1f, 0.9f, 64f);
        buttonLabel.color = new Color(0.08f, 0.09f, 0.12f);
        replayButton.onClick.AddListener(PlayAgain);
        resultsPanel.SetActive(false);
    }

    private TextMeshProUGUI CreateLabel(string name, string text, Transform parent,
        float bottom, float top, float fontSize)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetBounds(rect, new Vector2(0.05f, bottom), new Vector2(0.95f, top));
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = scoreText.font;
        label.text = text;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = fontSize;
        label.fontSize = fontSize;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        return label;
    }

    private static void SetBounds(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (replayButton != null)
            replayButton.onClick.RemoveListener(PlayAgain);
        if (resultsPanel != null)
            Destroy(resultsPanel);
    }
}
