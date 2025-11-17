using UnityEngine;

public class InteractionTrigger : MonoBehaviour
{
    [SerializeField] private InteractionUI ui;
    [SerializeField] private string objectName = "Object";

    private bool playerInRange;
    private bool hasPressedF;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        hasPressedF = false;        // reset so UI shows again
        ui.Show(objectName);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        ui.Hide();
    }

    private void Update()
    {
        if (!playerInRange || hasPressedF) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            hasPressedF = true;
            ui.Hide();
            // put interaction logic here if needed
        }
    }
}
