class SurvivorBotDoFire
{
    protected DayZPlayerImplement m_Player;
    protected SurvivorBotBase m_Soldier;
    protected Weapon_Base m_Weapon;
	protected bool OnLift = false;
	protected float rand_timerShoot;
	protected vector hitPosWS;

    protected bool m_Reset;
    protected bool m_Hidden;
    protected bool m_Crouched;
	protected Magazine new_Magazine;
	protected ref array<string> magazine_array = new array<string>;
	
	protected bool isHit = false;
	protected Human mTarget;
	protected bool m_ProcessLiftWeapon;

    void SurvivorBotDoFire(DayZPlayerImplement m_Owner, SurvivorBotBase m_SoldierOwner) { m_Player = m_Owner; m_Soldier = m_SoldierOwner; OnEntry(); }
    void ~SurvivorBotDoFire() { OnExit(); }

    void OnEntry()
    {
       GetGame().GetCallQueue(CALL_CATEGORY_SYSTEM).CallLater( OnWeapon, 1000, false );
	   m_Hidden = false;
	   m_Crouched = false;
    }

    void OnExit()
    {
		Print("[esbfsm] OnExit " + this);
    }
	
    void OnWeapon()
    {
        if (!GetWeapon())
		{
			m_Weapon = Weapon_Base.Cast(m_Soldier.GetHumanInventory().CreateInHands( m_Soldier.GetWeaponName() ));
			if (m_Weapon)
			{
				GetGame().ConfigGetTextArray ("cfgWeapons " + m_Weapon.GetType() + " magazines", magazine_array);
			}
		}

        if (m_Weapon)
        {
			for (int i = 0; i < m_Soldier.m_AttClassName.Count(); ++i)
			{	
				m_Weapon.GetInventory().CreateInInventory(m_Soldier.m_AttClassName.Get(i));		
			}
			
			new_Magazine = Magazine.Cast(m_Soldier.GetInventory().CreateInInventory(magazine_array.Get(Math.RandomInt(0, magazine_array.Count()))));	
			m_Soldier.GetDayZPlayerInventory().PostWeaponEvent( new WeaponEventAttachMagazine(m_Soldier, new_Magazine) );		
        } 
    }

    void OnFire(PlayerBase Target, bool Warning = false)
    {
	//	if (m_Soldier.IsBot()) m_Soldier.GetHcm().ForceStance(DayZPlayerConstants.STANCEIDX_RAISEDERECT);

        m_Soldier.SetTarget(Target);
    }

    void OnShoot()
    {		
        auto magAtt = m_Weapon.GetAttachmentByConfigTypeName( "DefaultMagazine" );
		
		SyncFire(m_Weapon);
        if( magAtt )
        {
            auto mag = Magazine.Cast( magAtt );

			if( mag && mag.IsMagazine() )
			{
				mag.ServerSetAmmoMax();
			}
	    }
		
		m_Soldier.GetWeaponManager().Fire(m_Weapon);
               
    }
	
	ref array<string> arraySoundSetShot = new array<string>;
	
	void SyncFire(Weapon m_isWeapon)
	{
		GetGame().ConfigGetTextArray ("cfgWeapons " + m_isWeapon.GetType() + " FullAuto " + "soundSetShot", arraySoundSetShot);
		
		if (arraySoundSetShot)
		{
			GetGame().ConfigGetTextArray ("cfgWeapons " + m_isWeapon.GetType() + " SemiAuto " + "soundSetShot", arraySoundSetShot);
		}
		
		if (GetGame().IsServer())
		{
			array<Man> players = new array<Man>;
			GetGame().GetPlayers( players );
			for (int i = 0; i < players.Count(); ++i)
			{
				PlayerBase t_Player = PlayerBase.Cast(players.Get(i));
				
				if (m_isWeapon)
				{
					if (vector.Distance(t_Player.GetPosition(), m_isWeapon.GetPosition()) < 1000)
					{
						ScriptRPC m_RPC = new ScriptRPC();
						m_RPC.Write(m_isWeapon);
						m_RPC.Write(arraySoundSetShot.Get(0));
						m_RPC.Send(t_Player, M_RPCc.BOT_ON_SHOOT, true, t_Player.GetIdentity());					
					}
				}
			}
			arraySoundSetShot.Clear();
		}
	}
	
	void ProcessLiftWeapon()
	{
		if (m_ProcessLiftWeapon)
		{

			
		//	HumanCommandWeapons	hcw = m_Soldier.GetCommandModifier_Weapons();
		//	if( hcw )
		//		hcw.LiftWeapon(true);
			
			m_ProcessLiftWeapon = false;
		}
	}
	
	float CalculateAimUD()
	{
		float m_AimSpeed = 0;
		if (m_Soldier)
		{
			float aimUD = m_Soldier.GetHcw().GetBaseAimingAngleUD();
		
			if (aimUD > 50)
			{
				m_AimSpeed = -0.5;
				return m_AimSpeed;
			}
			if (aimUD > 10)
			{
				m_AimSpeed =- 0.1;
				return m_AimSpeed;
			}
			if (aimUD > 0)
			{
				m_AimSpeed = -0.01;
				return m_AimSpeed;
			}
			if (aimUD < -50)
			{
				m_AimSpeed = 0.5;
				return m_AimSpeed;
			}
			if (aimUD < -10 )
			{
				m_AimSpeed = 0.1;
				return m_AimSpeed;
			}
			if (aimUD < 0)
			{
				m_AimSpeed = 0.01;
				return m_AimSpeed;
			}
		}
		return m_AimSpeed;
	}
	
	float CalculateAimLR()
	{
		float m_AimSpeed;
		float aimLR = m_Soldier.GetHcw().GetBaseAimingAngleLR();
		
		if (aimLR > 50)
		{
			m_AimSpeed = -0.5;
			return m_AimSpeed;
		}
		if (aimLR > 10)
		{
			m_AimSpeed =- 0.1;
			return m_AimSpeed;
		}
		if (aimLR > 0)
		{
			m_AimSpeed = -0.01;
			return m_AimSpeed;
		}
		if (aimLR < -50)
		{
			m_AimSpeed = 0.5;
			return m_AimSpeed;
		}
		if (aimLR < -10 )
		{
			m_AimSpeed = 0.1;
			return m_AimSpeed;
		}
		if (aimLR < 0)
		{
			m_AimSpeed = 0.01;
			return m_AimSpeed;
		}
		if (aimLR == 0)
		{
			m_AimSpeed = 0;
		}
		return m_AimSpeed;
	}

    void OnLift()
    {
				
        if (m_Soldier.GetHcm() && m_Soldier.IsBot()) 
        {		
			HumanMovementState b_State = new HumanMovementState;
			m_Soldier.GetMovementState(b_State);
			m_ProcessLiftWeapon = true;
			ProcessLiftWeapon();

			
			if (!b_State.IsRaised())
			{
				m_Soldier.GetCommand_Move().ForceStance(DayZPlayerConstants.STANCEIDX_RAISEDERECT);
			}
			OnLift = true;
        }

        GetGame().GetCallQueue(CALL_CATEGORY_SYSTEM).CallLater( m_Soldier.SetIronsights, 500, false, true );
		
    }
	
	void SoldierFight()
	{
		float dist;
		int randomHit;
		vector pTargetPos;
		vector m_SoldierPos;
		if (!mTarget) return;
		
		if (mTarget.GetPosition())
		{
			pTargetPos = mTarget.GetPosition();
			m_SoldierPos = m_Soldier.GetPosition();
								
			dist = vector.Distance( pTargetPos, m_SoldierPos );
			
			randomHit = Math.RandomInt(1, 15);
			
			if (randomHit > 4)
			{
				isHit = true;
			}
			else
			{
				isHit = false;
			}
			
			if (dist < 1.5)
			{
				HumanCommandMove b_cm = m_Soldier.GetCommand_Move();
				HumanCommandMelee2 b_hmc2 = m_Soldier.GetCommand_Melee2();
				m_Soldier.StartCommand_Melee2(mTarget, isHit, 1.2);
				if (b_cm)
				{
					b_cm.StartMeleeEvade();		
				}
				if (b_hmc2)
				{
					b_hmc2.ContinueCombo(isHit, 1.2);		
				}
				
				hitPosWS = mTarget.ModelToWorld(mTarget.GetDefaultHitPosition());
				int randDamag = Math.RandomInt(1, 20);
				if (randDamag > 18)
				{
					DamageSystem.CloseCombatDamage(mTarget, mTarget, randDamag, "MeleeZombie", hitPosWS);
				}
			}
		}
	}

	void OnUpdate (float dt) 
    {
        if (m_Soldier.GetTarget() && m_Soldier.GetTarget().IsAlive() && m_Soldier.IsAlive()) 
		{
            m_Reset = true;
            float m_Distance = vector.Distance(m_Soldier.GetTarget().GetPosition(), m_Soldier.GetPosition());
			if ((m_Distance < m_Soldier.GetDistance() || (m_Soldier.IsSniper() && (m_Distance < 1000))) && m_Soldier.GetDoTargetingFSM().IsVisible() && IsFacingTarget(m_Soldier.GetTarget())) 
			{
               
				if (m_Weapon) {
					OnLift = false;
					int ___M___ = m_Weapon.GetCurrentMuzzle();
					if (m_Weapon.CanFire(___M___)) { 
						
                        Human Target = Human.Cast(m_Soldier.GetTarget());
                        string Bullet = GetGame().ConfigGetTextOut( "CfgMagazines " + m_Weapon.GetChamberAmmoTypeName(___M___) + " " + "ammo" );
                       	OnLift();					
						if (Math.RandomInt(1, 50) == 1) {		
							if (m_Soldier.GetTarget().IsInherited(PlayerBase) && IsFacingTarget(m_Soldier.GetTarget())) {
                                IEntity vehicle = m_Soldier.GetTarget().GetParent();		                             
								if (OnLift)
								{	
									m_Soldier.GetInputController().OverrideAimChangeX(true, 0);
									m_Soldier.GetInputController().OverrideAimChangeY(true, CalculateAimUD());
									OnLift = false;
									OnShoot();
									hitPosWS = Target.ModelToWorld(Target.GetDefaultHitPosition());
									int randDamag = Math.RandomInt(2, 20);
									if (Math.RandomInt(1, m_Soldier.GetAcuracy()) == 1)
										DamageSystem.CloseCombatDamage(Target, Target, randDamag, Bullet, hitPosWS);					
								}
							}
						}
						if (Math.RandomInt(1, 5) == 1) {		
							if (m_Soldier.GetTarget().IsInherited(ZombieBase)) {
								ZombieBase Infected = ZombieBase.Cast(m_Soldier.GetTarget());
								GetGame().ObjectDelete( Infected );
							}	
						}					
                    }
                }
                else 
				{
					if (m_Soldier.GetItemInHands() && m_Soldier.GetItemInHands().IsInherited(Weapon)) {
                   //     m_Weapon = Weapon_Base.Cast(m_Soldier.GetItemInHands());	
                    } 
				//	OnLift();
					if (m_Soldier.GetTarget().IsInherited(ZombieBase)) {
						ZombieBase InfectedM = ZombieBase.Cast(m_Soldier.GetTarget());
						GetGame().ObjectDelete( InfectedM );
					}
					else
					{
						mTarget = Human.Cast(m_Soldier.GetTarget());
					//	if (OnLift)
					//	{
							GetGame().GetCallQueue(CALL_CATEGORY_GAMEPLAY).CallLater( SoldierFight, 500, false );
					//		OnLift = false;
					//	}
					}
				}
            }
            else
			{
				
                if (m_Distance > (m_Soldier.GetDistance() * 4)) {
					
                    OnReset();
                }   
                else 
				{
					
					 if (m_Soldier.IsRush() && m_Distance < (m_Soldier.GetDistance())) 
					{
		
						float distFromWeap = 0;
						if (mTarget)
							distFromWeap = vector.Distance( mTarget.GetPosition(), m_Soldier.GetPosition() );
						
						if (m_Weapon)
						{
							if (distFromWeap < 30)
							{
								if (m_Soldier.GetTarget())
									m_Soldier.GetDoMoveFSM().SetTargetWeap(m_Soldier.GetTarget().GetPosition(), true, false, 1, 30.0);
							}
							else
							{
								if (m_Soldier.GetTarget())
									m_Soldier.GetDoMoveFSM().SetTarget(m_Soldier.GetTarget().GetPosition(), true, false, 1, 1.0);
							}
						}
						else
						{
							if (m_Soldier.GetTarget())
								m_Soldier.GetDoMoveFSM().SetTarget(m_Soldier.GetTarget().GetPosition(), true, false, 1, 0.2);
						}
                    }
                } 
            }  
        }
        else 
		{
		
            OnReset();
        }
    }

    void OnHide()
    {
        if (!m_Hidden && m_Soldier.GetTarget() && m_Soldier.GetTarget().IsAlive()) {
            float m_Distance = vector.Distance(m_Soldier.GetTarget().GetPosition(), m_Soldier.GetPosition());
            if (m_Distance < m_Soldier.GetDistance()) {
                m_Soldier.GetDoMoveFSM().SetTarget(m_Soldier.GetDoIdleFSM().CalculateRandomPosition());
                m_Hidden = true;
            }
        }
    }

    void OnRunaway()
    {
        if (!m_Crouched && m_Soldier.GetTarget() && m_Soldier.GetTarget().IsAlive()) {
            float m_Distance = vector.Distance(m_Soldier.GetTarget().GetPosition(), m_Soldier.GetPosition());
            if (m_Distance < m_Soldier.GetDistance()) {
                m_Soldier.GetDoMoveFSM().SetTarget(m_Soldier.GetDoIdleFSM().CalculateRandomPosition());
                if (m_Soldier.GetHcm()) 
                {
                    GetGame().GetCallQueue(CALL_CATEGORY_SYSTEM).CallLater( m_Soldier.GetCommand_Move().ForceStance, 10000, false, DayZPlayerConstants.STANCEIDX_RAISEDPRONE );
                    m_Crouched = true;
                }
            }
        }
    }

    void OnReset()
    {
        if (m_Reset)
        {

            if (m_Soldier.GetTarget())
            {
                m_Soldier.SetTarget(null);
            } 


            if (m_Soldier.GetHcw())
            {
                m_Soldier.GetHcw().LiftWeapon(false);
			//	m_Soldier.GetHcw().SetADS(false);
            }
			
			m_Soldier.GetInputController().OverrideRaise(true, true);
			
            m_Soldier.GetDoMoveFSM().SetMove();

            m_Reset = false;
        }
    }

    bool IsFacingTarget( Object Target )
	{
		vector pdir = m_Soldier.GetDirection();
		vector ptv = Target.GetPosition() - m_Soldier.GetPosition();

		pdir.Normalize();
		ptv.Normalize();

		if (Math.AbsFloat(pdir[0] - ptv[0]) < 0.8 && Math.AbsFloat(pdir[2] - ptv[2]) < 0.8 )
		{
			return true;
			//SendLog("IsFacingTarget true");
			
		}

		return false;
	}
 	void SendLog(string message) 
	{ 
		Print("AI BOT LOG: " + message);
	}
	
    void SetHidden(bool State) { m_Hidden = State; }
    void SetCrouched(bool State) { m_Crouched = State; }
    void SetWeapon(Weapon_Base Weap) { m_Weapon = Weap; }

    bool GetHidden() { return m_Hidden; }
    bool GetCrouched() { return m_Crouched; }
    
    Weapon_Base GetWeapon() { return m_Weapon; }
}