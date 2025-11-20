using UnityEngine;
using System.Collections;

public class StudentManager : MonoBehaviour
{
    public static StudentManager Instance;

    private StudentController[] students;
    private StudentController activeStudent;
    private bool isActionRunning = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optionally DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // find all students automatically
        students = FindObjectsByType<StudentController>(FindObjectsSortMode.None);
    }

    void Update()
    {
        // Manual test: Press E to make a random student stand for 5 seconds
        if (Input.GetKeyDown(KeyCode.E) && !isActionRunning)
        {
            StartCoroutine(RandomStudentStand());
        }
    }

    /// <summary>
    /// Manual random stand for 5 seconds (used for testing only)
    /// </summary>
    IEnumerator RandomStudentStand()
    {
        isActionRunning = true;

        // pick random student
        activeStudent = students[Random.Range(0, students.Length)];

        // make him stand
        activeStudent.ToggleSitStand(false);

        // wait 5 seconds
        yield return new WaitForSeconds(5f);

        // make him sit again
        activeStudent.ToggleSitStand(true);

        isActionRunning = false;
    }

    /// <summary>
    /// Makes a random student stand during audio playback duration
    /// Called from GeminiLLM
    /// </summary>
    public IEnumerator StandForAudio(float duration)
    {
        if (isActionRunning) yield break;
        isActionRunning = true;

        // pick random student
        activeStudent = students[Random.Range(0, students.Length)];

        // stand before audio starts
        activeStudent.ToggleSitStand(false);

        // wait for audio to finish
        yield return new WaitForSeconds(duration);

        // sit back
        activeStudent.ToggleSitStand(true);

        isActionRunning = false;
    }
}
