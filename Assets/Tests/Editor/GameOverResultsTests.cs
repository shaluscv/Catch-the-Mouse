using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PUBLIC_INTERFACE
/// <summary>Regression checks for the controller reachable from the Game scene.</summary>
public class GameOverResultsTests
{
    private const string Key = "CatchTheMouse.PersonalBest";
    private GameObject canvasObject;
    private GameObject mouseObject;
    private Component mouse;
    private Type mouseType;
    private bool hadBest;
    private int savedBest;

    [SetUp]
    // PUBLIC_INTERFACE
    /// <summary>Isolates the preference key and creates the controller's UI dependencies.</summary>
    public void SetUp()
    {
        hadBest = PlayerPrefs.HasKey(Key);
        savedBest = PlayerPrefs.GetInt(Key, 0);
        PlayerPrefs.DeleteKey(Key);
        mouseType = Type.GetType("Mouse, Assembly-CSharp", true);
        canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(GraphicRaycaster));
        GameObject label = new GameObject("Score", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(canvasObject.transform, false);
        mouseObject = new GameObject("Mouse", typeof(AudioSource), typeof(CircleCollider2D));
        mouseObject.SetActive(false);
        mouse = mouseObject.AddComponent(mouseType);
        mouseType.GetField("scoreText").SetValue(mouse, label.GetComponent<TextMeshProUGUI>());
        mouseType.GetField("upSpeed").SetValue(mouse, 0.05f);
    }

    private void Begin(int? previousBest = null)
    {
        if (previousBest.HasValue)
            PlayerPrefs.SetInt(Key, previousBest.Value);
        Invoke("Awake");
        Invoke("Start");
    }

    private void Invoke(string name)
    {
        mouseType.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(mouse, null);
    }

    private T Read<T>(string name)
    {
        return (T)mouseType.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mouse);
    }

    [TearDown]
    // PUBLIC_INTERFACE
    /// <summary>Restores local data and removes all test-created objects.</summary>
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(canvasObject);
        UnityEngine.Object.DestroyImmediate(mouseObject);
        if (hadBest)
            PlayerPrefs.SetInt(Key, savedBest);
        else
            PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    [Test]
    // PUBLIC_INTERFACE
    /// <summary>Checks startup, accepted catches, and the strict loss threshold.</summary>
    public void PlayingPreservesCatchesAndStrictThreshold()
    {
        Begin();
        Assert.IsFalse(Read<GameObject>("resultsPanel").activeSelf);
        Assert.AreEqual("0", mouseType.GetField("scoreText").GetValue(mouse) is TextMeshProUGUI label
            ? label.text : null);
        for (int i = 0; i < 3; i++)
            Invoke("OnMouseDown");
        Assert.AreEqual(3, Read<int>("score"));
        mouseObject.transform.position = new Vector2(0, 6f);
        Invoke("Update");
        Assert.IsFalse(Read<bool>("gameOver"));
        mouseObject.transform.position = new Vector2(0, 6.01f);
        Invoke("Update");
        Assert.IsTrue(Read<bool>("gameOver"));
        Assert.AreEqual("Final Score: 3", Read<TextMeshProUGUI>("finalScoreText").text);
        Assert.AreEqual(3, PlayerPrefs.GetInt(Key));
    }

    [TestCase(null, 0, 0)]
    [TestCase(5, 3, 5)]
    [TestCase(5, 5, 5)]
    [TestCase(5, 7, 7)]
    [TestCase(-8, 0, 0)]
    [TestCase(-8, 2, 2)]
    // PUBLIC_INTERFACE
    /// <summary>Checks missing, negative, lower, tied, and record personal bests.</summary>
    public void ResultsUseHighestNonnegativeBest(int? previous, int catches, int expected)
    {
        Begin(previous);
        for (int i = 0; i < catches; i++)
            Invoke("OnMouseDown");
        mouseObject.transform.position = new Vector2(0, 7);
        Invoke("Update");
        Assert.AreEqual("Personal Best: " + expected, Read<TextMeshProUGUI>("bestScoreText").text);
        if (expected > 0)
            Assert.AreEqual(expected, PlayerPrefs.GetInt(Key));
    }

    [Test]
    // PUBLIC_INTERFACE
    /// <summary>Rejects a late catch and all further gameplay after loss.</summary>
    public void LateCatchCannotRescueRunAndResultsRemainStable()
    {
        Begin();
        mouseObject.transform.position = new Vector2(0, 7);
        Invoke("OnMouseDown");
        GameObject panel = Read<GameObject>("resultsPanel");
        for (int i = 0; i < 3; i++)
        {
            Invoke("Update");
            Invoke("FixedUpdate");
            Invoke("OnMouseDown");
        }
        Assert.AreEqual(0, Read<int>("score"));
        Assert.AreEqual(new Vector3(0, 7, 0), mouseObject.transform.position);
        Assert.AreSame(panel, Read<GameObject>("resultsPanel"));
        Assert.IsTrue(panel.activeSelf);
        Assert.IsFalse(mouseObject.GetComponent<Collider2D>().enabled);
    }
}
