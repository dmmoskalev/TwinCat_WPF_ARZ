   M S B A M L   `   `   `  ????E  AWpfTwinCAT, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:WpfTwinCAT.PagesWpfTwinCAT.Pages  5      $    WpfTwinCAT.Pages.DiscreteXPage   5      W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35U QWPFToolkit, Version=3.5.40128.1, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation       5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      0localclr-namespace:WpfTwinCAT.Pages     5      -   5          N? Title$  DiscreteXPage??5       ? 	Resources 5       	  OKBtn ?? 
TargetType ?? Property ?? Value ?? 
TargetType ??Name ?? CornerRadius ?? Triggers	 P? Property
 P? Value ?? 
TargetName  ValueBtn  
ControlBtn??%?  &	        &	 ?    &	 ?    ?? 5   
   " ??6   .?5      ??  ?@?OverridesDefaultStyle$
 True??66   ?? 5       ? ?? ?	#FFFAF7F76+   ?? 5       ? ??$ 20(?6)   ?? 5       ? ??$ 0q?6'   ?? 5       ? ?? 5      ?? 5      " ??6&   .R?5      ?? $ border??6!   $??2q?61   ???	#FFFFC000#??z??6]   $??40??5   !   $	??100??6-   $ 3??69   ._?5      ?? $??Center=?6/   $??Center=?6L   5       5      P? 5      	 ? ??$

 True??6;   . ?5   "   ?? $ border??6)    ? ?? ?	#FF3030306S   ?? 5   "    ? ??
 ??  ?6?   5      P? 5      	 ? ??$
 False??6;   . ?5   "   ?? $ border??6)    ? ??
 ??  ?6S   5       5"      5$      5%      ?? 5&   
   " ??6!   .?5'      ??  ?@?OverridesDefaultStyle$
 True??66   ?? 5(       ? ?? ?	#FF5959596+   ?? 5)       ? ?? ?	#FFFAF7F76+   ?? 5*       ? ??$ 20(?6)   ?? 5+       ? ??$ Right=?64   ?? 5,       ? ?? 5-      ?? 5.      " ??6&   .R?5/      ?? $ border??6!   $??3q?61   ???	#FFFFC000#??z??6]   $??70??50   !   $	??120??6-   $ 5??69   ._?51      ?? $??Center=?6/   $??Center=?6L   52      53      55      56      ?? 58   
   " ??6#   .?59      ??  ?@?OverridesDefaultStyle$
 True??66   ?? 5:       ? ?? ?	#FF5959596+   ?? 5;       ? ?? ?	#FFFAF7F76+   ?? 5<       ? ??$ 20(?6)   ?? 5=       ? ??$
 Left=?64   ?? 5>       ? ??$ 10,0,0,0q?6'   ?? 5?       ? ?? 5@      ?? 5A      " ??6&   .R?5B      ?? $ border??6!   $??3q?61   ???	#FFFFC000#??z??6]   $??70??5C   !   $??70??6-   $ 5??68   ._?5D      ?? $??Center=?6/   $??Center=?6L   5E      5F      5H      5I      5K      .??5L      ? ???	#FF404040$??Stretch=?6"    ? RowDefinitions 5M   
   ?? 5N      $??30 ?6   ?? 5O      $??30 ?6   ?? 5P      $??80 ?6   ?? 5Q      $	??100 ?6   ?? 5R      $??20 ?6   ?? 5S      $??30 ?6   ?? 5T      $??30 ?6   ?? 5U      $??80 ?6   ?? 5V      $	??100 ?6    ? ColumnDefinitions 5X   
   ?? 5Y      $??30 ?6   ?? 5Z      $	??140 ?6   ?? 5[      $??90 ?6   ?? 5\      $	??170 ?6   ?? 5]      $??90 ?6   ?? 5^      $	??170 ?6   ?? 5_      $??90 ?6   ?? 5`      $	??170 ?6   ?? 5a      $	??110 ?6   ?? 5b      $??30 ?6   .+?5e   
   ?? -   5         ??Name$ Grpb_DotMove??5e      $??0??6)   $??1??66   $??4??6F   $??8??6W   ???	#FFFFC000$??1q?5f   +   ???	#FFFBFAFA$??20(?6V   $??Center=?5g      $??Disabled=?67   $??LeftToRight=?6k   $??Stretch=?5h      $	??Top=?61    ? UseLayoutRounding$ False??6P    ? Cursor$ No??6j   ??5i      ?? 5j      ???	#FFFFC000$??1q?63   $??	20,0,50,0q?6H   .??5k      ??  ?? Orientation$ 
Horizontal=?6!   .+?5l      ?? $??/Images/track_na.png??6    $??30??6>   $??30??6I   $??Center=?6U   ?? 5m      $B??< Движение на точку по пресетам (м)??6    ???	#FFFBFAFA$??20(?6d   $??Center=?6r   5n      5o      5s      ?? 5t   
   -   5        $ Grpb_DistMove??5t      $??5??6*   $??1??67   $??4??6G   $??8??6X   ???	#FFFFC000$??1q?5u   +   ???	#FFFBFAFA$??20(?6V   $??Center=?5v      $??Disabled=?67   $??LeftToRight=?6k   $??Stretch=?5w      $	??Top=?61   $ False??6P   $ No??6j   ??5x      ?? 5y      ???	#FFFFC000$??1q?63   $??	20,0,50,0q?6H   .??5z      ?? $ 
Horizontal=?6!   .+?5{      ?? $??/Images/track_na.png??6    $??30??6>   $??30??6I   $??Center=?6U   ?? 5|      $L??F Движение на расстояние по пресетам (м)??6    ???	#FFFBFAFA$??20(?6i   $??Center=?6w   5}      5      5?      ?? 5?   
   -   5        $ GrDistBoarder??5?      $??3q?6(   ???	#FFFFC000???	#FF595959$??5??6k   $??2??6y   $??7??6?   $??2??6?   $??40??5?   !   $	??200??6-   $ 5??69   $	??Top=?6J   ._?5?      ?? -   5         ??Name$ LvTRPositionValue??5?      $??	123456789??6/   ???	#FFFBFAFA$??20(?6Z   $??Center=?6h   $??Center=?6?   5?      ?? 5?   
   -   5         ??Name$ BtDotPreset0??5?      $	??0.0??6'   $??2??65   $??1??6B   #??[ 6k   ?? 5?   
   -   5        $ 	BtDotSet0??5?      $??2??6$   $??2??61   #??[ 6U   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -   5        $ BtDotPreset1??5?      $	??1.0??6'   $??2??65   $??3??6B   #??[ 6k   ?? 5?   
   -	   5        $ 	BtDotSet1??5?      $??2??6$   $??4??61   #??[ 6U   .??5?      ?? $??/Images/settings_na.png??6   $??60??69   $??60??6D   5?      ?? 5?   
   -
   5        $ BtDotPreset2??5?      $	??0.0??6'   $??2??65   $??5??6B   #??[ 6k   ?? 5?   
   -   5        $ 	BtDotSet2??5?      $??2??6$   $??6??61   #??[ 6U   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -   5        $ BtDotPreset3??5?      $	??1.0??6'   $??2??65   $??7??6B   #??[ 6k   ?? 5?   
   -   5        $ 	BtDotSet3??5?      $??2??6$   $??8??61   #??[ 6U   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -   5        $ BtDotPreset4??5?      $	??0.0??6'   $??3??65   $??1??6B   #??[ 6k   ?? 5?   
   -   5        $ 	BtDotSet4??5?      $??3??6$   $??2??61   #??[ 6U   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -   5        $ BtDotPreset5??5?      $	??1.0??6'   $??3??65   $??3??6B   #??[ 6k   ?? 5?   
   -   5        $ 	BtDotSet5??5?      $??3??6$   $??4??61   #??[ 6U   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -   5        $ BtDotPreset6??5?      $	??0.0??6'   $??3??65   $??5??6B   #??[ 6k   ?? 5?   
   -   5        $ 	BtDotSet6??5?      $??3??6$   $??6??61   #??[ 6U   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -   5        $ BtDotPreset7??5?      $	??1.0??6'   $??3??65   $??7??6B   #??[ 6k   ?? 5?   
   -   5        $ 	BtDotSet7??5?      $??3??6$   $??8??61   #??[ 6U   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -   5        $ BtDistPreset0??5?      $	??0.0??6(   $??7??66   $??1??6C   #??[ 6l   ?? 5?   
   -   5        $ 
BtDistSet0??5?      $??7??6%   $??2??62   #??[ 6V   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -   5        $ BtDistPreset1??5?      $	??0.0??6(   $??7??66   $??3??6C   #??[ 6l   ?? 5?   
   -   5        $ 
BtDistSet1??5?      $??7??6%   $??4??62   #??[ 6V   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -   5        $ BtDistPreset2??5?      $	??0.0??6(   $??7??66   $??5??6C   #??[ 6l   ?? 5?   
   -   5        $ 
BtDistSet2??5?      $??7??6%   $??6??62   #??[ 6V   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -   5        $ BtDistPreset3??5?      $	??0.0??6(   $??7??66   $??7??6C   #??[ 6l   ?? 5?   
   -   5        $ 
BtDistSet3??5?      $??7??6%   $??8??62   #??[ 6V   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -   5        $ BtDistPreset4??5?      $	??0.0??6(   $??8??66   $??1??6C   #??[ 6l   ?? 5?   
   -   5        $ 
BtDistSet4??5?      $??8??6%   $??2??62   #??[ 6V   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -    5        $ BtDistPreset5??5?      $	??0.0??6(   $??8??66   $??3??6C   #??[ 6l   ?? 5?   
   -!   5        $ 
BtDistSet5??5?      $??8??6%   $??4??62   #??[ 6V   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -"   5        $ BtDistPreset6??5?      $	??0.0??6(   $??8??66   $??5??6C   #??[ 6l   ?? 5?   
   -#   5        $ 
BtDistSet6??5?      $??8??6%   $??6??62   #??[ 6V   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   -$   5        $ BtDistPreset7??5?      $	??0.0??6(   $??8??66   $??7??6C   #??[ 6l   ?? 5?   
   -%   5        $ 
BtDistSet7??5?      $??8??6%   $??8??62   #??[ 6V   .??5?      ?? $??/Images/settings_na.png??6   $??60??65   $??60??6@   5?      ?? 5?   
   $??/Images/move_right.png??6   $??40??60   $??40??6<   $??7??6G   $??1??6T   $
??Left=?6d   ?? 5?   
   $??/Images/move_left.png??6   $??40??6/   $??40??6;   $??8??6F   $??1??6S   $
??Left=?6c   ?? 5?   
   $??/Images/move_right.png??6   $??40??60   $??40??6<   $??7??6G   $??3??6T   $
??Left=?6d   $??30,0,0,0q?6   ?? 5?   
   $??/Images/move_left.png??6   $??40??6/   $??40??6;   $??8??6F   $??3??6S   $
??Left=?6c   $??30,0,0,0q?6~   ?? 5?   
   $??/Images/move_right.png??6   $??40??60   $??40??6<   $??7??6G   $??5??6T   $
??Left=?6d   $??30,0,0,0q?6   ?? 5?   
   $??/Images/move_left.png??6   $??40??6/   $??40??6;   $??8??6F   $??5??6S   $
??Left=?6c   $??30,0,0,0q?6~   ?? 5?   
   $??/Images/move_right.png??6   $??40??60   $??40??6<   $??7??6G   $??7??6T   $
??Left=?6d   $??30,0,0,0q?6   ?? 5?   
   $??/Images/move_left.png??6   $??40??6/   $??40??6;   $??8??6F   $??7??6S   $
??Left=?6c   $??30,0,0,0q?6~   ?? 5?   
   -&   5         ??Name$ TxbErrorMsg??5?      $??1??6)   $??5??66   $??4??6F   
???  ??$??20(?6k   ?? 5?   
   -'   5        $ 
TxbInfoMsg??5?      $??1??6(   $??5??65   $??4??6E   
???????$??20(?6l   ?? 5?   
   -(   5        $ BtnTestStart??5?      $??7??69   $??9??6F   $??Start??6V   $??30??6f   ?? 5?   
   -)   5        $ BtnTestStop??5?      $??8??67   $??9??6D   $
??Stop??6T   $??30??6c   ?? 5?   
   -*   5        $ BrdAlarmInfo??5?      ???	#FFFFC600$??2q?6@   $ 10??6T   
???  ?? K? ZIndex$ 10??5?      $??2??6#   $??6??60   $??2??6A   $??6??6Q   $y?Hidden=?6e   $??50,40,100,10q?5?      ._?5?      ?? $ Vertical=?6   .+?5?      ?? -+   5        $ TxbAlarmInfo??5?      
???????$??20(?6F   $
??Wrap=?6T   $??10,100,10,60q?6h   $??Center=?6~   '%АВАРИЙНАЯ ОСТАНОВКА5?      ?? 5?      -,   5        $ BtInfoOK??5?      $??Закрыть??6G   #??[  6Y   5?      5?      5?      5?      