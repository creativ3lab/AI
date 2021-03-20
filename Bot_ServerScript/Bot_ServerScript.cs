class CfgMods
{
	class RusFlex_ServerScripts
	{
		dir = "RusFlex_ServerScripts";
		picture = "";
		action = "";
		hideName = 1;
		hidePicture = 1;
		name = "";
		credits = "";
		author = "RusFlex";
		authorID = "0";
		version = 1.0;
		extra = 0;
		type = "mod";
		dependencies[] = 
		{
			"Game",
			"World",
			"Mission"
		};
		class defs
		{
			class gameScriptModule
			{
				value = "";
				files[] = 
				{
					"RusFlex_ServerScripts/3_Game"
				};
			};
			class worldScriptModule
			{
				value = "";
				files[] =
				{
					"RusFlex_ServerScripts/4_World"
				};
			};
			class missionScriptModule
			{
				value = "";
				files[] = 
				{
					"RusFlex_ServerScripts/5_Mission"
				};
			};
		};
	};
};
class CfgPatches
{
	class RusFlex_ServerScripts
	{
		units[] = {};
		weapons[] = {};
		requiredVersion = 0.1;
		requiredAddons[] = {};
	};
};
