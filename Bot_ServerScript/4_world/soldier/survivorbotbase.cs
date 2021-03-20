class SurvivorBotBase extends PlayerBase
{
	protected EntityAI m_Target;
	protected HumanInputController m_Hic;
	protected HumanItemAccessor m_Hia;
	protected HumanCommandWeapons m_Hcw;
	protected HumanCommandMove m_Hcm;
	
	protected ref SurvivorBotDoFire m_DoFire;
	protected ref SurvivorBotDoMove m_DoMove;
	protected ref SurvivorBotDoTargeting m_DoTargeting;
	protected ref SurvivorBotDoIdle m_DoIdle;
	protected ref SurvivorJumpClimb m_DoJump;

	protected vector m_BeginPosition;
	protected vector m_EndPosition;
	protected vector m_CurrentPosition;
	
	protected bool m_PassiveBot;
	protected bool m_HeroTraderBot;
	protected bool m_BanditTraderBot;
	protected bool m_IdleBot;
	protected bool m_SniperBot;
	protected bool m_RushBot;
	protected bool m_onRespawned;
	bool m_IsVoice;
	
	protected float m_AcuracyBot;
	protected float m_DistanceBot;
	protected float m_RandomBot;
	protected float m_TimeBot;
    protected bool m_UseCheckpoint;

	protected string m_MagazineName;
	protected string m_WeaponName;
	
    ref array<string> m_AttClassName = new array<string>;
	
    ref array<vector> m_ArrayCheckpoint = new array<vector>;;
	
    void SurvivorBotBase() { SetEventMask(EntityEvent.SIMULATE | EntityEvent.INIT); }
    void ~SurvivorBotBase() { }

	override void EOnSimulate(IEntity owner, float dt) { if (IsAlive()) OnUpdate(dt); }
	override void EOnInit( IEntity other, int extra) { OnEntry(); }

    void OnEntry()
    {
	//	Print("[esbfsm] OnEntry " + this);

		PlayerBase l_Player;
		Class.CastTo(l_Player, this);
		
		BotNameGenerator.BotNameGeneratorInsert();
		
		m_AcuracyBot = 5;
		m_DistanceBot = 100;

		m_RandomBot = 5;
        m_TimeBot = 10000;

		m_PassiveBot = false;
		m_UseCheckpoint = false;
		m_HeroTraderBot = false;
		m_BanditTraderBot = false;
		m_SniperBot = false;
		m_IsVoice = false;
		m_RushBot = true;
		m_IdleBot = true;
		m_IsBot = true;
		m_onRespawned = true;
		m_MagazineName = "";
		m_WeaponName = "";

		m_BeginPosition = GetPosition();
        m_EndPosition = "0 0 0";
        m_CurrentPosition = GetPosition();

		m_RGSManager = new RandomGeneratorSyncManager(l_Player);
		m_WeaponManager = new WeaponManager(l_Player);

		GetGame().GetCallQueue(CALL_CATEGORY_SYSTEM).CallLater( OnFsm, 1000, false );
		
		m_BotName = BotNameGenerator.GetNameRandom();
    }
	
	void OnFsm()
	{
		StartCommand_Move();

		m_DoMove = new SurvivorBotDoMove(this, this);
		m_DoFire = new SurvivorBotDoFire(this, this);
		m_DoTargeting = new SurvivorBotDoTargeting(this, this);
		m_DoIdle = new SurvivorBotDoIdle(this, this);
		m_DoJump = new SurvivorJumpClimb(this, this);
		
		m_Hic = GetInputController();
		m_Hia = GetItemAccessor();
		m_Hcw = GetCommandModifier_Weapons();
		m_Hcm = GetCommand_Move();
	}

	void OnExit() 
    {
		Print("[esbfsm] OnExit " + this);

		m_BeginPosition = "0 0 0";
		m_EndPosition = GetPosition();
		m_CurrentPosition = "0 0 0";

		if (m_DoFire)
			delete m_DoFire;
		
		if (m_DoMove)
			delete m_DoMove;

		if (m_DoTargeting)
			delete m_DoTargeting;

		if (m_DoIdle)
			delete m_DoIdle;
		
		if (m_DoJump)
			delete m_DoJump;
		
		if (m_WeaponManager)
		//	delete m_WeaponManager;

		if (m_RGSManager)
			delete m_RGSManager;

		SetTarget(null);
 
            
		SetEventMask(EntityEvent.INIT);
		this = NULL;
    }

	void OnUpdate(float dt) 
    {
		static float l_dtAccumulator;

		l_dtAccumulator += dt;
		if (l_dtAccumulator >= 0.01)
		{
			if (m_DoFire)
				m_DoFire.OnUpdate( dt );

			if (m_DoMove)
				m_DoMove.OnUpdate( dt );
			
			if (m_DoTargeting)
				m_DoTargeting.OnUpdate( dt );

			if (m_DoIdle)	
				m_DoIdle.OnUpdate( dt );

			if (GetHealth("", "") < 50)
			{
				if (GetHealth("", "") > 25)
					m_DoFire.OnHide();
				else
					m_DoFire.OnRunaway();
			}		

			l_dtAccumulator = 0;	
		}
    }

	override void EEKilled( Object killer )
	{
		Object posSound = Object.Cast(GetGame().CreateObject( "SoundPos", this.GetPosition()));
		GetGame().CreateSoundOnObject(posSound, "Bot_death_" + Math.RandomInt(1, 4).ToString(), 40, false);

		PlayerBase m_deadBot = this;
		string m_deadBotName = m_deadBot.GetNameBot();

		string KillFeed_Text = "[БОТ]" + m_deadBotName + " умер";
		
	//	SendMessageKillFeed(KillFeed_Text);
			
	//	SimulateDead();
		
		OnDeath();
	}
	
	bool bot_CanJump()
	{
		
		if( IsFBSymptomPlaying() || IsRestrained() )
			return false;
		
		if( m_MovementState.m_iStanceIdx == DayZPlayerConstants.STANCEIDX_PRONE || m_MovementState.m_iStanceIdx == DayZPlayerConstants.STANCEIDX_RAISEDPRONE)
			return false;
		
		HumanItemBehaviorCfg b_hibcfg = GetItemAccessor().GetItemInHandsBehaviourCfg();
		if( !b_hibcfg.m_bJumpAllowed )
			return false;
		
		return true;
	}
	
	void bot_OnJumpStart()
	{
		m_WeaponManager.DelayedRefreshAnimationState(10);
	}
	
	void bot_OnJumpEnd(int b_pLandType = 0)
	{
		if(m_PresenceNotifier)
		{
			switch(b_pLandType)
			{
			case HumanCommandFall.LANDTYPE_NONE:
			case HumanCommandFall.LANDTYPE_LIGHT:
				m_PresenceNotifier.ProcessEvent(EPresenceNotifierNoiseEventType.LAND_LIGHT);
				break;
			case HumanCommandFall.LANDTYPE_MEDIUM:
			case HumanCommandFall.LANDTYPE_HEAVY:
				m_PresenceNotifier.ProcessEvent(EPresenceNotifierNoiseEventType.LAND_HEAVY);
				break;
			}
		}
		
		m_WeaponManager.RefreshAnimationState();
	}
	
	bool bot_CanClimb( int bot_climbType, SHumanCommandClimbResult bot_climbRes)
	{
		if( IsFBSymptomPlaying() || IsRestrained() )
			return false;
		
		if( m_MovementState.m_iStanceIdx == DayZPlayerConstants.STANCEIDX_PRONE || m_MovementState.m_iStanceIdx == DayZPlayerConstants.STANCEIDX_RAISEDPRONE)
			return false;
		
		HumanItemBehaviorCfg b_hibcfg = GetItemAccessor().GetItemInHandsBehaviourCfg();
		if( !b_hibcfg.m_bJumpAllowed )
			return false;
		
		if(bot_climbRes)
		{
			EntityAI entity;
			if (Class.CastTo(entity,bot_climbRes.m_GrabPointParent) && entity.IsHologram())
				return false;
			if (Class.CastTo(entity,bot_climbRes.m_ClimbStandPointParent) && entity.IsHologram())
				return false;
			if (Class.CastTo(entity,bot_climbRes.m_ClimbOverStandPointParent) && entity.IsHologram())
				return false;
		}

		return true;	
	}
			
	
	void SimulateDead()
	{
		ItemBase item = ItemBase.Cast(GetHumanInventory().GetEntityInHands());

		if (item && !item.IsInherited(SurvivorBase))	
		{
			ServerDropEntity(item);
			item.SetSynchDirty();
		}
		
		array<EntityAI> itemsArray = new array<EntityAI>;
		ItemBase itemDublicat;
		GetInventory().EnumerateInventory(InventoryTraversalType.PREORDER, itemsArray);
		
		for (int i = 0; i < itemsArray.Count(); i++)
		{
			Class.CastTo(itemDublicat, itemsArray.Get(i));
			if (itemDublicat)
			{
				string itemName = itemDublicat.GetType();
				GetGame().CreateObject( itemName, GetPosition());
			}
		}
		
		itemsArray.Clear();	
     // 	RemoveAllItems();
		SetPosition("0 0 0");
	}
	
	void OnDeath()
	{
		Print("[esbfsm] OnDeath " + this);

		OnExit();
	}
	
	override void EEHitBy(TotalDamageResult damageResult, int damageType, EntityAI source, int component, string dmgZone, string ammo, vector modelPos, float speedCoef)
	{
       super.EEHitBy(damageResult, damageType, source, component, dmgZone, ammo, modelPos, speedCoef);
	   OnHit(damageResult, damageType, source, component, dmgZone, ammo, modelPos);
	}
	
	void OnHit(TotalDamageResult damageResult, int damageType, EntityAI source, int component, string dmgZone, string ammo, vector modelPos)
	{
	//	Print("[esbfsm] OnHit " + this);
		if (this.IsAlive())
		{
			if (source && source != this) 
			{
				if ( !source.IsInherited(CarScript) )
				{
					if (!m_IsVoice)
					{
						m_IsVoice = true;
						GetGame().CreateSoundOnObject(this, "Bot_friendly_fire_" + Math.RandomInt(1, 5).ToString(), 40, false);
						GetGame().GetCallQueue(CALL_CATEGORY_GAMEPLAY).CallLater(VoiceEnd, 7000, false); 
					}
					m_DoTargeting.OnResetTarget();
					SetTarget(PlayerBase.Cast(source.GetHierarchyRoot()));
					//m_DoFire.OnFire(PlayerBase.Cast(source.GetHierarchyRoot()), true);
				}
			}
		}
	}
	
	void SetPassive(bool Result) { m_PassiveBot = Result; }
	void SetUseCheckpoint() { m_UseCheckpoint = true; m_IdleBot = false; }
	void SetBandit(bool Result) { m_BanditTraderBot = Result; }
	void SetHero(bool Result) { m_HeroTraderBot = Result; }
	void SetIdle(bool Result) { m_IdleBot = Result; }
	void SetSniper(bool Result) { m_SniperBot = Result; }
	void SetRush(bool Result) { m_RushBot = Result; }
	void SetAcuracy(float Result) { m_AcuracyBot = Result; }
	void SetRandom(float Result) { m_RandomBot = Result; }
	void SetTime(float Result) { m_TimeBot = Result; }
	void SetDistance(float Result) { m_DistanceBot = Result; }
	void SetMagazine(string Mag) { m_MagazineName = Mag; }
	void AddWeapon(string Weap) { m_WeaponName = Weap; }
	void SetBeginPosition(vector Result) { m_BeginPosition = Result; }
	void SetRespawned(bool Result) { m_onRespawned = Result; }
	
	bool IsPassive() { return m_PassiveBot; }
	bool IsIdle() { return m_IdleBot; }
	bool IsSniper() { return m_SniperBot; }
	bool IsRush() { return m_RushBot; }
	bool IsBanditTrader() { return m_BanditTraderBot; }
	bool IsHeroTrader() { return m_HeroTraderBot; }
    bool IsUseCheckpoint() { return m_UseCheckpoint; }
	bool IsRespawned() { return m_onRespawned; }
	
	float GetAcuracy() { return m_AcuracyBot; }
	float GetDistance() { return m_DistanceBot; }
	float GetRandom() { return m_RandomBot; }
	float GetTime() { return m_TimeBot; }

	string GetMagazineName() { return m_MagazineName; }
	string GetWeaponName() { return m_WeaponName; }
	vector GetBeginPosition() { return m_BeginPosition; }
	vector GetEndPosition() { return m_EndPosition; }

	EntityAI GetTarget() { return m_Target; }

	HumanInputController GetHic() { return m_Hic; }
	HumanItemAccessor GetHia() { return m_Hia; }
	HumanCommandWeapons GetHcw() { return m_Hcw; }
	HumanCommandMove GetHcm() { return m_Hcm; }

	SurvivorBotDoMove GetDoMoveFSM() { return m_DoMove; }
	SurvivorBotDoFire GetDoFireFSM() { return m_DoFire; }
	SurvivorBotDoTargeting GetDoTargetingFSM() { return m_DoTargeting; }
	SurvivorBotDoIdle GetDoIdleFSM() { return m_DoIdle; }
	SurvivorJumpClimb GetDoJumpFSM() { return m_DoJump; }

	
    void VoiceEnd()
	{
		m_IsVoice = false;
	}	
	
	void AddWeaponAtt(string className)
	{
		m_AttClassName.Insert(className);
	}
	
	void AddCheckpoint(vector m_Checkpoint)
	{
		if (m_UseCheckpoint)
			m_ArrayCheckpoint.Insert(m_Checkpoint);
	}
	

 	void SetTarget(EntityAI Target) 
	{
		if (!m_IsVoice && this.IsAlive())
		{
			m_IsVoice = true;
			
			GetGame().CreateSoundOnObject(this, "Bot_enemy_" + Math.RandomInt(1, 12).ToString(), 40, false);
			
			GetGame().GetCallQueue(CALL_CATEGORY_GAMEPLAY).CallLater(VoiceEnd, 7000, false); 
		}
		
		m_Target = Target; 
	}
	
	override void OnStoreSave( ParamsWriteContext ctx )
	{
			if (IsBot())
			return;
	}

	override void AfterStoreLoad()
	{
		if (IsBot())
			return;
	}

	override void OnStoreSaveLifespan( ParamsWriteContext ctx )
	{		
		if (IsBot())
			return;
	}

	
	override void OnPlayerLoaded()
	{
		if (IsBot())
			return;
	}
	
	override void OnConnect()
	{
		if (IsBot())
			return;
	}
	
	override void OnReconnect()
	{
		if (IsBot())
			return;
	}
	
	override void OnDisconnect()
	{
		if (IsBot())
			return;		
	}
}