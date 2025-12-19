using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public class BossPhase0Controller : MonoBehaviour
{
    [Header("Dialogue")]
    [Tooltip("Kéo component UI quản lý dialogue của bạn vào đây (MonoBehaviour bất kỳ).")]
    [SerializeField] private MonoBehaviour dialogueUI;

    [SerializeField] private ScriptableObject preSummonDialogue;
    [SerializeField] private ScriptableObject postSummonDialogue;
    [SerializeField] private string bossName = "???";

    [Header("Summon")]
    [SerializeField] private GameObject summonedEnemyPrefab;
    [SerializeField] private Transform summonSpawnPoint;
    [SerializeField] private GameObject summonVfxPrefab;
    [SerializeField] private float summonDelay = 0.35f;

    [Header("Phase 0 Lock")]
    [SerializeField] private Behaviour[] disableWhileLocked;
    [SerializeField] private Collider2D[] disableCollidersWhileLocked;
    [SerializeField] private EnemyHealth phase0Health;

    [Header("Phase Switch")]
    [SerializeField] private GameObject phase0RootToDeactivate;
    [SerializeField] private GameObject phase1RootToActivate;
    [SerializeField] private GameObject phase1SpawnerTriggerToEnable;

    [Header("Events")]
    public UnityEvent onPhase0Started;
    public UnityEvent onSummonSpawned;
    public UnityEvent onPhase1Activated;

    private bool _running;

    public void StartPhase0()
    {
        if (_running) return;

        if (summonedEnemyPrefab == null)
        {
            Debug.LogError($"{nameof(BossPhase0Controller)}: summonedEnemyPrefab is missing.", this);
            return;
        }

        if (dialogueUI == null)
            Debug.LogWarning($"{nameof(BossPhase0Controller)}: dialogueUI is NULL -> sẽ không hiện panel dialog.", this);

        if (preSummonDialogue == null)
            Debug.LogWarning($"{nameof(BossPhase0Controller)}: preSummonDialogue is NULL -> sẽ bỏ qua dialog 1.", this);

        if (postSummonDialogue == null)
            Debug.LogWarning($"{nameof(BossPhase0Controller)}: postSummonDialogue is NULL -> sẽ bỏ qua dialog 2.", this);

        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        _running = true;
        onPhase0Started?.Invoke();

        LockPhase0(true);

        // 1) Dialog 1
        yield return DialogueReflectionUtils.PlayDialogueIfPossible(dialogueUI, preSummonDialogue, bossName);

        // 2) VFX + delay
        Vector3 spawnPos = summonSpawnPoint != null ? summonSpawnPoint.position : transform.position;

        GameObject vfx = null;
        if (summonVfxPrefab != null)
            vfx = Instantiate(summonVfxPrefab, spawnPos, Quaternion.identity);

        if (summonDelay > 0f)
            yield return new WaitForSeconds(summonDelay);

        // 3) Spawn enemy con
        GameObject enemy = Instantiate(summonedEnemyPrefab, spawnPos, Quaternion.identity);
        onSummonSpawned?.Invoke();

        if (vfx != null) Destroy(vfx);

        // 4) Chờ enemy con chết
        yield return WaitUntilSummonedEnemyDies(enemy);

        // 5) Dialog 2
        yield return DialogueReflectionUtils.PlayDialogueIfPossible(dialogueUI, postSummonDialogue, bossName);

        // 6) Switch phase (same frame)
        ActivatePhase1AndDeactivatePhase0SameFrame();

        _running = false;
    }

    private void LockPhase0(bool locked)
    {
        if (phase0Health != null)
            phase0Health.SetCanTakeDamage(!locked);

        if (disableWhileLocked != null)
            for (int i = 0; i < disableWhileLocked.Length; i++)
                if (disableWhileLocked[i] != null)
                    disableWhileLocked[i].enabled = !locked;

        if (disableCollidersWhileLocked != null)
            for (int i = 0; i < disableCollidersWhileLocked.Length; i++)
                if (disableCollidersWhileLocked[i] != null)
                    disableCollidersWhileLocked[i].enabled = !locked;
    }

    private IEnumerator WaitUntilSummonedEnemyDies(GameObject enemyGo)
    {
        if (enemyGo == null) yield break;

        bool died = false;

        EnemyHealth enemyHealth = enemyGo.GetComponentInChildren<EnemyHealth>();
        if (enemyHealth != null)
        {
            void OnDead() => died = true;
            enemyHealth.OnDeath += OnDead;

            while (!died && enemyGo != null)
                yield return null;

            enemyHealth.OnDeath -= OnDead;
        }
        else
        {
            while (enemyGo != null)
                yield return null;
        }
    }

    private void ActivatePhase1AndDeactivatePhase0SameFrame()
    {
        if (phase1SpawnerTriggerToEnable != null)
            phase1SpawnerTriggerToEnable.SetActive(true);

        if (phase1RootToActivate != null)
            phase1RootToActivate.SetActive(true);

        onPhase1Activated?.Invoke();

        GameObject root0 = phase0RootToDeactivate != null ? phase0RootToDeactivate : gameObject;
        root0.SetActive(false);
    }
}

public static class DialogueReflectionUtils
{
    private static readonly string[] MethodNames = { "ShowDialogue", "StartDialogue", "PlayDialogue", "Open" };
    private static readonly string[] RunningNames = { "IsOpen", "isOpen", "IsPlaying", "isPlaying", "IsRunning", "isRunning", "IsActive", "isActive" };

    public static IEnumerator PlayDialogueIfPossible(MonoBehaviour dialogueUI, ScriptableObject dialogueObject, string bossName)
    {
        if (dialogueUI == null || dialogueObject == null)
            yield break;

        bool invoked = TryInvokeDialogue(dialogueUI, dialogueObject, bossName);

        if (!invoked)
        {
            Debug.LogWarning($"DialogueReflectionUtils: Không gọi được dialogue method trên {dialogueUI.GetType().Name}. " +
                             $"Cần method ShowDialogue/StartDialogue/PlayDialogue/Open nhận (dialogue) hoặc (dialogue, string).",
                             dialogueUI);
            yield return null;
            yield break;
        }

        Func<bool> isRunning = CreateRunningGetter(dialogueUI, dialogueUI.GetType());
        if (isRunning == null)
        {
            yield return null; // không detect được trạng thái -> chờ 1 frame thôi
            yield break;
        }

        int safety = 0;
        while (!isRunning() && safety < 15)
        {
            safety++;
            yield return null;
        }

        while (isRunning())
            yield return null;
    }

    private static bool TryInvokeDialogue(MonoBehaviour dialogueUI, ScriptableObject dialogueObject, string bossName)
    {
        var uiType = dialogueUI.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var methodName in MethodNames)
        {
            // (dialogue, string)
            var m2 = FindBestMethod(uiType, flags, methodName, 2, dialogueObject);
            if (m2 != null)
            {
                var ps = m2.GetParameters();
                object arg0 = dialogueObject; // already compatible
                m2.Invoke(dialogueUI, new object[] { arg0, bossName });
                return true;
            }

            // (dialogue)
            var m1 = FindBestMethod(uiType, flags, methodName, 1, dialogueObject);
            if (m1 != null)
            {
                var ps = m1.GetParameters();
                object arg0 = dialogueObject;
                m1.Invoke(dialogueUI, new object[] { arg0 });
                return true;
            }
        }

        return false;
    }

    private static MethodInfo FindBestMethod(Type uiType, BindingFlags flags, string name, int paramCount, ScriptableObject dialogueObject)
    {
        var methods = uiType.GetMethods(flags);
        foreach (var m in methods)
        {
            if (m.Name != name) continue;

            var ps = m.GetParameters();
            if (ps.Length != paramCount) continue;

            // param0
            var p0 = ps[0].ParameterType;
            bool ok0 =
                p0 == typeof(object) ||
                p0 == typeof(ScriptableObject) ||
                p0.IsAssignableFrom(dialogueObject.GetType());

            if (!ok0) continue;

            // param1
            if (paramCount == 2 && ps[1].ParameterType != typeof(string))
                continue;

            return m;
        }
        return null;
    }

    private static Func<bool> CreateRunningGetter(object ui, Type uiType)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var n in RunningNames)
        {
            var p = uiType.GetProperty(n, flags);
            if (p != null && p.PropertyType == typeof(bool))
                return () => (bool)p.GetValue(ui);

            var f = uiType.GetField(n, flags);
            if (f != null && f.FieldType == typeof(bool))
                return () => (bool)f.GetValue(ui);
        }

        foreach (var n in RunningNames)
        {
            var m = uiType.GetMethod(n, flags, null, Type.EmptyTypes, null);
            if (m != null && m.ReturnType == typeof(bool))
                return () => (bool)m.Invoke(ui, null);
        }

        return null;
    }
}
