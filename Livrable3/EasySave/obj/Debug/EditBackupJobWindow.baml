   M S B A M L   `   `   `  ����  EasySaveV2corrigé,    &EasySave.Wpf.Views.EditBackupJobWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   $��Segoe UI)�5   	   $��12(�6   
�������  � WindowStartupLocation$  CenterOwner=�5	   	    � ShowInTaskbar .   � 
ResizeMode$ NoResize=�5   	    � SizeToContent$ WidthAndHeight=�5   	   $	��220��5   	   $	��470��6   $	��470��6(   .��5      � $��15q�6    � ColumnDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $
��Auto �6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   �� -   5         ��Name$ 	labelName��5      $��0��6'   $��0��64   $��Center=�6D   $��0,5,10,5q�6_   �� 5   
   -   5         ��Name$ textBoxName��5      $��0��6'   $��1��64   $��Center=�6D   $��0,5,0,5q�6_   ��5      �� *Name+ �� Mode$ TwoWay=�	 �� UpdateSourceTrigger$	 PropertyChanged=��� 5"   
   -   5        $ labelSource��5"      $��1��6)   $��0��66   $��Center=�6F   $��0,5,10,5q�6a   �� 5#   
   -   5        $ textBoxSourceDir��5#      $��1��6,   $��1��69   $��Center=�6I   $��0,5,0,5q�6d   ��5$      �� *SourceDirectory+$ TwoWay=�$	 PropertyChanged=��� 5%   
   -   5        
 ��Name$
 buttonBrowseSource��5%      $��1��6-   $��2��6:   $	��...��6J   $��30��6X   $��25��6c   $��Center=�6o   $��5,5,0,5q�6�   5&      �� 5)   
   -   5        $ labelTarget��5)      $��2��6)   $��0��66   $��Center=�6F   $��0,5,10,5q�6a   �� 5*   
   -   5        $ textBoxTargetDir��5*      $��2��6,   $��1��69   $��Center=�6I   $��0,5,0,5q�6d   ��5+      �� *TargetDirectory+$ TwoWay=�$	 PropertyChanged=��� 5,   
   -   5        $
 buttonBrowseTarget��5,      $��2��6-   $��2��6:   $	��...��6J   $��30��6X   $��25��6c   $��Center=�6o   $��5,5,0,5q�6�   5-      �� 50   
   -	   5        $ 	labelType��50      $��3��6'   $��0��64   $��Center=�6D   $��0,5,10,5q�6_   �� 51   
   -
   5         ��Name$ comboBoxBackupType��51      $��3��6/   $��1��6<   $��Center=�6L   $��0,5,0,5q�6g   $	��150��52   J   $
��Left=�6V   �� 55   
   $��4��6   $��0��6"   $��3��62    �� Orientation$ 
Horizontal=�6F   $��Right=�6_   $��0,15,0,0q�6{   .+�56      �� -   5        $
 buttonOK��56      $��95��6'   $��30��62   $��0,0,10,0q�6>    �� 	IsDefault . 
���q�<�
�������57   1   �� 58      -   5        $
 buttonCancel��58      $��95��6+   $��30��66    �� IsCancel . 
���\\��
�������59   ,   5:      5;      5<      