class CfgPatches
{
  class AI
  {
    Units[]={};
    Weapons[]={};
    RequiredVersion=0.1;
    RequiredAddons[]=
    {
      "DZ_Data",
      "DZ_AI"
    };
  };
};
class CfgMod
{
  class AI
  {
    Dir = "";
    Picture = "";
    Author = "";
    Action = "";
    HideName = "";
    HidePicture = "";
    Name = "";
    Credits = "";
    Version = "";
    Extra = "";
    Type = "";
    Dependencies[] = 
    {
      "Game",
      "World",
      "Mission"
     };
     class defs
     {
        class GameScriptModule
        { 
          Value = "";
          Files[] = {};
        }; 
        class WorldScriptModule
        { 
          Value = "";
          Files[] = {};
        }; 
        class MissionScriptModule
        {
          Value = "";
          Files[] = "";
        };
      };
    };
  };
};
