from pathlib import Path
import re
p=Path('Assets/Prototype/Scripts/PlayPage.cs');s=p.read_text(encoding='utf-8-sig');s=re.sub(r'(?:feedback|lootLabel|rhythmLabel)\.text\s*=.*?;', '', s, flags=re.S);s=s.replace('rhythmLabel, digLabel','digLabel').replace('depthLabel, lootLabel, feedback, autoAction','depthLabel, autoAction');p.write_text(s,encoding='utf-8-sig')
p=Path('Assets/Prototype/Editor/ExpeditionBuilder.cs');s=p.read_text(encoding='utf-8-sig');s=re.sub(r'^\s*page\.(?:lootLabel|feedback|rhythmLabel)=Text\(.*?;\n','\n',s,flags=re.M)
s=s.replace('"BurstCharge",220,1012,280,8','"BurstCharge",260,1182,200,8').replace('circle,215,1087,290,180','circle,215,1017,290,180').replace('circle,217,1058,286,180','circle,217,988,286,180').replace('circle,230,1040,260,166','circle,230,970,260,166')
s=s.replace('            Text(face.transform,"DigHint","꾹 누르기",0,98,260,35,19,Hex("77532B"),true,TextAlignmentOptions.Center);','').replace('"DigLabel","DIG",0,24,260,70','"DigLabel","DIG",0,40,260,80')
p.write_text(s,encoding='utf-8-sig')
