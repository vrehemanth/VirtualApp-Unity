using UnityEngine;
using System.Collections;

public class StudentController : MonoBehaviour
{
    Animator animator;
    private bool isSitting = true;

    private Vector3 sittingPosition = new Vector3(0f, -0.09f, 0.6f);
    private Vector3 standingPosition = new Vector3(0f, -0.2f, 0.4f);

    private Coroutine moveRoutine;

    void Start()
    {
        animator = GetComponent<Animator>();
        animator.SetBool("isSitting", isSitting);
        transform.localPosition = sittingPosition;
    }

    public void ToggleSitStand(bool sit)
    {
        if (animator.IsInTransition(0))
            return;

        isSitting = sit;
        animator.SetBool("isSitting", isSitting);

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        // always use 2.25 seconds
        moveRoutine = StartCoroutine(SmoothMove(isSitting ? sittingPosition : standingPosition, 2.25f));
    }

    IEnumerator SmoothMove(Vector3 targetPosition, float duration)
    {
        Vector3 start = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.localPosition = Vector3.Lerp(start, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = targetPosition;
    }
}
