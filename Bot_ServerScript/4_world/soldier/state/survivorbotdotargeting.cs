class SurvivorBotDoTargeting
{
    protected PlayerBase m_Player;
    protected SurvivorBotBase m_Soldier;

    ref Timer m_ResetTimer;
    ref Timer m_ZombieTimer;
    ref Timer m_TargetTimer;

    void SurvivorBotDoTargeting(PlayerBase m_Owner, SurvivorBotBase m_SoldierOwner) { m_Player = m_Owner; m_Soldier = m_SoldierOwner; OnEntry(); }
    void ~SurvivorBotDoTargeting() { OnExit(); }

    void OnEntry()
    {
	//	Print("[esbdt] OnEntry " + this);

        if (!m_TargetTimer) {
			m_TargetTimer = new Timer();
		}

		if (!m_ZombieTimer) {
			m_ZombieTimer = new Timer();
		}

		if (!m_ResetTimer) {
			m_ResetTimer = new Timer();
		}

        m_ResetTimer.Run(60, this, "OnResetTarget", NULL, true);
		m_TargetTimer.Run(2, this, "OnSelectTarget", NULL, true);
		m_ZombieTimer.Run(1, this, "OnSelectZombie", NULL, true);
    }

	void OnExit () 
    {
        Print("[esbdt] OnExit " + this);

        if (m_ResetTimer)
        {
            m_ResetTimer.Stop();
            delete m_ResetTimer;
        }

        if (m_TargetTimer)
        {
            m_TargetTimer.Stop();
            delete m_TargetTimer;
        }

        if (m_ZombieTimer)
        {
            m_ZombieTimer.Stop();
            delete m_ZombieTimer;
        }
    }

    void OnTargeting()
	{
    }

	void OnUpdate (float dt) 
    {
    }

    bool IsVisible()
	{
		if (m_Soldier.GetTarget()) 
        {
			int headIndex = m_Soldier.GetBoneIndexByName("Head");
			
			vector rayStart = m_Soldier.GetBonePositionWS(headIndex);
			vector rayEnd = m_Soldier.GetTarget().GetPosition() + Vector(0, 1, 0);

   			auto objs = GetObjectsAt( rayStart, rayEnd, m_Soldier );

			if (objs) 
            {
				if( objs.Count() > 0 ) 
                {
        			if (objs[0] == m_Soldier.GetTarget() || objs[0].IsInherited(DayZPlayer) || objs[0].IsTransport()) 
                    {
						if (!m_Soldier.m_IsVoice)
						{
							m_Soldier.m_IsVoice = true;
							GetGame().CreateSoundOnObject(m_Soldier, "Bot_attack_" + Math.RandomInt(1, 9).ToString(), 40, false);
							GetGame().GetCallQueue(CALL_CATEGORY_GAMEPLAY).CallLater(m_Soldier.VoiceEnd, 7000, false); 
						}	
						return true;
					}
    			}
			}
		}
 		else if (!m_Soldier.m_IsVoice)
		{
			m_Soldier.m_IsVoice = true;
			GetGame().CreateSoundOnObject(m_Soldier, "Bot_search_" + Math.RandomInt(1, 12).ToString(), 40, false);
			GetGame().GetCallQueue(CALL_CATEGORY_GAMEPLAY).CallLater(m_Soldier.VoiceEnd, 7000, false); 
		}	
		
		return false;
	}
	
    set< Object > GetObjectsAt( vector from, vector to, Object ignore = NULL, float radius = 0.5, Object with = NULL )
	{
		vector contact_pos;
		vector contact_dir;
		int contact_component;

		set< Object > geom = new set< Object >;
		set< Object > view = new set< Object >;

		DayZPhysics.RaycastRV( from, to, contact_pos, contact_dir, contact_component, geom, with, ignore, false, false, ObjIntersectGeom, radius );
		DayZPhysics.RaycastRV( from, to, contact_pos, contact_dir, contact_component, view, with, ignore, false, false, ObjIntersectView, radius );

		if ( geom.Count() > 0 ) 
		{
			return geom;
		}
		if ( view.Count() > 0 ) 
		{
			return view;
		}
		return NULL;
	}
	
 	void SendLog(string message) 
	{ 
		Print("AI BOT LOG: " + message);
	}
	
    bool IsEntityVisible(EntityAI Target)
	{
		if (Target) 
        {
			int headIndex = m_Soldier.GetBoneIndexByName("Head");
			
			vector rayStart = m_Soldier.GetBonePositionWS(headIndex);
			vector rayEnd = Target.GetPosition() + Vector(0, 1, 0);

   			auto objs = GetObjectsAt( rayStart, rayEnd, m_Soldier );
		//	SendLog(objs.ToString());
			if (objs) 
            {
				if( objs.Count() > 0 ) 
                {
        			if (objs[0] == Target || objs[0].IsInherited(DayZPlayer) || objs[0].IsTransport() || objs[0].IsBush()) 
                    {
					//	SendLog(objs.ToString());
						return true;
					}
    			}
			}
		}

		return false;
	}

    void OnSelectZombie()
	{
		array<Object> objects = new array<Object>;
		array<CargoBase> proxyCargos = new array<CargoBase>;

		GetGame().GetObjectsAtPosition(m_Soldier.GetPosition(), m_Soldier.GetDistance(), objects, proxyCargos);

		int c = objects.Count();
		for (int i = 0; i < c; i++)
		{
			Object o = objects[i];
			if (o == m_Soldier)
				continue;

			if (m_Soldier.GetTarget())
				continue;

			if (!o.IsInherited(ZombieBase))
				continue;

			if (!o.IsAlive())
				continue;

			if (!IsEntityVisible(EntityAI.Cast(o)))
				continue;

			m_Soldier.SetTarget(EntityAI.Cast(o));
		}
	}

    void OnSelectTarget()
	{
		if (m_Soldier.IsPassive())
			return;

		float min_dist = 999999;
		int min_index = -1;

		array<Object> objects = new array<Object>;
		array<CargoBase> proxyCargos = new array<CargoBase>;

		GetGame().GetObjectsAtPosition(m_Soldier.GetPosition(), m_Soldier.GetDistance(), objects, proxyCargos);

		int c = objects.Count();
		for (int i = 0; i < c; i++)
		{
			Object o = objects[i];
			if (o == m_Soldier)
				continue;

			if (!o.IsEntityAI())
				continue;

			if (!o.IsMan())
				continue;

			if (!o.IsAlive())
				continue;

			if (o.IsInherited(SurvivorBotBase))
				continue;

			PlayerBase target = PlayerBase.Cast(o);

			if (!IsEntityVisible(target))
				continue;

			float d = vector.Distance(o.GetPosition(), m_Soldier.GetPosition());
			if ( d < min_dist )
			{
				min_dist = d;
				min_index = i;
			}
		}

		if (min_index != -1)
		{
			m_Soldier.SetTarget(DayZPlayer.Cast( objects.Get(min_index) ) );
		}
	}

    void OnResetTarget() 
	{ 
		if (m_Soldier.GetTarget() && !IsVisible()) 
		{ 
			m_Soldier.SetTarget(null);
			
			m_Soldier.GetDoMoveFSM().ResetWaypoints();
			
			if (!m_Soldier.GetDoMoveFSM().GetMove() && m_Soldier.IsBot())
            {
                m_Soldier.GetDoMoveFSM().SetMove();
            }

			if (m_Soldier.GetHcm() && m_Soldier.IsBot()) 
        	{
            //	m_Soldier.GetHcm().ForceStance(DayZPlayerConstants.STANCEIDX_RAISEDERECT);
        	}
		} 
	}
}