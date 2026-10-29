using UnityEngine;

public enum GameplayAction { Move, Look, Fire, WeaponChange, Reload, Skill, Interact }

// Local intent only. Never use this to gate network receive, remote interpolation,
// timers, authoritative inventory updates or round/reconnect restoration.
public static class GameplayActionPolicy
{
    public static bool MenuBlocksInput =>
        (SettingsMenu.Instance != null && SettingsMenu.Instance.IsOpen) ||
        (GameController.Instance != null && GameController.Instance.IsPaused) ||
        InGameChatUI.BlocksGameplayInput || VectorVoicePanel.BlocksGameplayInput;

    public static bool CanAct(Component owner, GameplayAction action, bool includeMenus = true)
    {
        if (owner == null || (includeMenus && MenuBlocksInput)) return false;
        if (includeMenus && Game.Pings.PingSystem.WheelOpen && action != GameplayAction.Move && action != GameplayAction.Look) return false;
        var practice = PracticePlayerContext.For(owner);
        if (includeMenus && practice != null && practice.range.MenuOpen) return false;
        if (owner.GetComponentInParent<PlayerHP>()?.IsDead == true ||
            owner.GetComponentInParent<PlayerDeathController>()?.IsDead == true) return false;
        var net = NetworkManager.Instance;
        if (practice == null && net != null && (net.ReconnectBusy || !net.IsConnected || !net.IsAuthenticated)) return false;
        if (practice == null && GameController.Instance != null &&
            (GameController.Instance.CurrentState == GameController.GameFlowState.Pick ||
             GameController.Instance.CurrentState == GameController.GameFlowState.Loading)) return false;
        if (action == GameplayAction.Look) return !InitiatorSkill_Q.IsLocalDroneMode;
        if (action == GameplayAction.Move) return !InitiatorSkill_Q.IsLocalDroneMode;
        if (owner.GetComponentInParent<PlayerSpike>()?.IsInteracting == true && action != GameplayAction.Interact) return false;
        if (owner.GetComponentInParent<PlayerMovement>()?.IsServerStunned == true ||
            owner.GetComponentInParent<ElectricShock>()?.IsStunned == true) return false;
        if (practice == null && InitiatorSkill_Q.IsLocalDroneMode) return false;
        if (practice == null && RoundManager.Instance != null &&
            (RoundManager.Instance.currentState == RoundManager.RoundState.RoundEnd ||
             RoundManager.Instance.currentState == RoundManager.RoundState.GameOver)) return false;
        if (InitiatorSkill_Q.IsLocalReturning) return false;
        var entry = owner.GetComponentInParent<EntrySkillPresentation>();
        if (entry != null && entry.Recovering && !(
            (owner is EntrySkill_C && entry.CurrentCast == EntrySkillPresentation.Cast.Dash) ||
            (owner is EntrySkill_Q && entry.CurrentCast == EntrySkillPresentation.Cast.Smoke) ||
            (owner is EntrySkill_E && entry.CurrentCast == EntrySkillPresentation.Cast.Stealth))) return false;
        if (owner.GetComponentInParent<ControllerSkill_X>()?.IsCasting == true && !(owner is ControllerSkill_X)) return false;
        if (owner.GetComponentInParent<ControllerSkill_C>()?.IsCasting == true && !(owner is ControllerSkill_C)) return false;
        if (owner.GetComponentInParent<ControllerSkill_Q>()?.IsCasting == true && !(owner is ControllerSkill_Q)) return false;
        if (ControllerSkill_E.IsBlockingWeaponInput && !(owner is ControllerSkill_E)) return false;
        if (owner.GetComponentInParent<InitiatorSkill_C>()?.BlocksActions == true && !(owner is InitiatorSkill_C)) return false;
        if (owner.GetComponentInParent<InitiatorSkill_E>()?.BlocksActions == true && !(owner is InitiatorSkill_E)) return false;
        if (owner.GetComponentInParent<InitiatorSkill_X>()?.IsCasting == true && !(owner is InitiatorSkill_X)) return false;
        if (SentinelSkill_C.IsSelectingSentinelC && !(owner is SentinelSkill_C)) return false;
        if (SentinelSkill_E.IsSelectingSentinelE && !(owner is SentinelSkill_E)) return false;
        if ((SentinelSkill_X.IsSelectingSentinelX || SentinelSkill_X.IsChargingSentinelX) && !(owner is SentinelSkill_X)) return false;
        if(owner.GetComponentInParent<SentinelCastMotion>()?.Recovering==true &&
            !(owner is SentinelSkill_C) && !(owner is SentinelSkill_Q) && !(owner is SentinelSkill_E) && !(owner is SentinelSkill_X))return false;
        if (action == GameplayAction.Skill) return !InitiatorSkill_Q.IsLocalPreparing || owner is InitiatorSkill_Q;
        if (InitiatorSkill_Q.IsLocalPreparing) return false;
        if (practice == null && (EntrySkill_X.IsLocalBladeMode ||
            owner.GetComponentInParent<EntryDashMotor>()?.IsDashing == true ||
            InitiatorSkill_C.ShouldBlockWeaponInput() || SentinelSkill_C.ShouldBlockWeaponInput() ||
            SentinelSkill_E.ShouldBlockWeaponInput() || SentinelSkill_X.ShouldBlockWeaponInput() ||
            ControllerSkill_E.IsBlockingWeaponInput || ControllerSkill_E.IsSelectingSmoke || ControllerSkill_E.IsSmokeMapOpen)) return false;
        if (action == GameplayAction.Fire && StorePanelUI.IsBlockingGameplayInput) return false;
        return true;
    }
}
