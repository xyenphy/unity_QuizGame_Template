[System.Serializable]
public class QuestionData
{
    public string subtopic;
    public string questionText;
    public string[] options; // Array of 4 strings
    public int correctIndex; // 0, 1, 2, or 3
}