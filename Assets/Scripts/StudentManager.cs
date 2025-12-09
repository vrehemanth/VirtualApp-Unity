using UnityEngine;
using UnityEngine.InputSystem;   // NEW INPUT SYSTEM
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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        students = FindObjectsByType<StudentController>(FindObjectsSortMode.None);
    }

    void Update()
    {
        // MANUAL TEST: Press E to make a random student stand for 5 seconds
        if (!isActionRunning && Keyboard.current != null && 
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartCoroutine(RandomStudentStand());
        }
    }

    IEnumerator RandomStudentStand()
    {
        isActionRunning = true;

        activeStudent = students[Random.Range(0, students.Length)];

        activeStudent.ToggleSitStand(false);

        yield return new WaitForSeconds(5f);

        activeStudent.ToggleSitStand(true);

        isActionRunning = false;
    }

    public IEnumerator StandForAudio(float duration)
    {
        if (isActionRunning) yield break;
        isActionRunning = true;

        activeStudent = students[Random.Range(0, students.Length)];

        activeStudent.ToggleSitStand(false);

        yield return new WaitForSeconds(duration);

        activeStudent.ToggleSitStand(true);

        isActionRunning = false;
    }
}
