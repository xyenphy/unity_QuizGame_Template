using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionTextDisplay;
    public TMP_Text scoreTextDisplay;
    public TMP_Text timerTextDisplay; // Added for the timer
    public Button[] optionButtons;

    [Header("Data")]
    public List<QuestionData> allQuestions = new List<QuestionData>();
    private List<QuestionData> quizQueue = new List<QuestionData>();

    [Header("Feedback Colors")]
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color neutralColor = Color.white;

    [Header("Settings")]
    public float timePerQuestion = 10f;
    private float currentTime;
    private bool isAnswering = false;

    private int score = 0;
    private int currentQuestionIndex = 0;

    void Start()
    {
        LoadQuestionsFromCSV();
        GenerateQuiz();
    }

    void Update()
    {
        if (isAnswering)
        {
            currentTime -= Time.deltaTime;
            timerTextDisplay.text = "" + Mathf.CeilToInt(currentTime).ToString();

            if (currentTime <= 0)
            {
                isAnswering = false;
                StartCoroutine(HandleFeedback(-1)); // -1 indicates time ran out
            }
        }
    }

    void LoadQuestionsFromCSV()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Questions.csv");

        if (File.Exists(filePath))
        {
            string fileContent = File.ReadAllText(filePath);
            string[] lines = fileContent.Split(new string[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            Debug.Log("Total lines detected: " + lines.Length);

            // START AT 0 because your file has NO header row
            for (int i = 0; i < lines.Length; i++)
            {
                // This Regex splits by comma but ignores commas inside double quotes
                string[] data = Regex.Split(lines[i], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

                if (data.Length >= 7)
                {
                    try
                    {
                        QuestionData q = new QuestionData();
                        q.subtopic = data[0].Trim().Trim('"');

                        // Question text (Removing quotes if they exist)
                        q.questionText = data[1].Trim().Trim('"');

                        // Options (Removing quotes if they exist)
                        q.options = new string[]
                        {
                        data[2].Trim().Trim('"'),
                        data[3].Trim().Trim('"'),
                        data[4].Trim().Trim('"'),
                        data[5].Trim().Trim('"')
                        };

                        // Correct Index
                        string indexString = data[6].Trim().Trim('"');
                        q.correctIndex = int.Parse(indexString);

                        allQuestions.Add(q);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error parsing line {i + 1}: {e.Message}. Data: {lines[i]}");
                    }
                }
            }
            Debug.Log("Successfully loaded " + allQuestions.Count + " questions.");
        }
    }

    void GenerateQuiz()
    {
        var grouped = allQuestions.GroupBy(q => q.subtopic).ToList();
        Debug.Log("Found " + grouped.Count + " unique subtopics.");

        foreach (var group in grouped)
        {
            var selected = group.OrderBy(x => Random.value).Take(2).ToList();
            Debug.Log("Subtopic: " + group.Key + " | Questions available: " + group.Count() + " | Selected: " + selected.Count);
            quizQueue.AddRange(selected);
        }

        quizQueue = quizQueue.OrderBy(x => Random.value).ToList();
        Debug.Log("Final Quiz Queue Size: " + quizQueue.Count);

        DisplayQuestion();
    }

    void DisplayQuestion()
    {
        if (currentQuestionIndex < quizQueue.Count)
        {
            QuestionData currentQ = quizQueue[currentQuestionIndex];
            questionTextDisplay.text = currentQ.questionText;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionButtons[i].GetComponentInChildren<TMP_Text>().text = currentQ.options[i];
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
            }

            currentTime = timePerQuestion;
            isAnswering = true;
        }
        else
        {
            EndQuiz();
        }
    }

    // ONLY ONE OnOptionSelected METHOD NOW
    void OnOptionSelected(int choiceIndex)
    {
        if (!isAnswering) return;
        isAnswering = false;
        StartCoroutine(HandleFeedback(choiceIndex));
    }

    IEnumerator HandleFeedback(int choiceIndex)
    {
        SetButtonsInteractable(false);
        int correctIdx = quizQueue[currentQuestionIndex].correctIndex;

        if (choiceIndex == correctIdx)
        {
            score += 10;
            scoreTextDisplay.text = "" + score;
            optionButtons[choiceIndex].image.color = correctColor;
        }
        else
        {
            if (choiceIndex != -1)
                optionButtons[choiceIndex].image.color = wrongColor;

            optionButtons[correctIdx].image.color = correctColor;
        }

        yield return new WaitForSeconds(1.5f);

        ResetButtonColors();
        currentQuestionIndex++;
        SetButtonsInteractable(true);
        DisplayQuestion();
    }

    void ResetButtonColors()
    {
        foreach (var btn in optionButtons) btn.image.color = neutralColor;
    }

    void SetButtonsInteractable(bool state)
    {
        foreach (var btn in optionButtons) btn.interactable = state;
    }

    void EndQuiz()
    {
        isAnswering = false;
        questionTextDisplay.text = "Quiz Finished!";

        // Clear timer and score display for the final screen if desired
        timerTextDisplay.text = "";

        // Hide buttons so player can see the final result
        foreach (var btn in optionButtons) btn.gameObject.SetActive(false);

        Debug.Log("Final Score: " + score);

        // Start the restart countdown
        StartCoroutine(RestartAfterDelay(3.0f));
    }

    IEnumerator RestartAfterDelay(float delay)
    {
        // Optional: Update the UI to show a countdown
        float timer = delay;
        while (timer > 0)
        {
            timerTextDisplay.text = "Restarting in " + Mathf.CeilToInt(timer);
            yield return new WaitForSeconds(1.0f);
            timer--;
        }

        // Reload the currently active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}