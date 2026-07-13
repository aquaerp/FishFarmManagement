# دليل الأنماط البصرية - AquaFarm Pro WPF
## Visual Style Guide & Design System

**الإصدار**: 1.0  
**تاريخ**: 14 أكتوبر 2025  
**الحالة**: مرجع تصميم معتمد

---

## 🎨 نظام الألوان (Color Palette)

### الألوان الأساسية (Primary Colors)

#### Deep Blue - اللون الأساسي الأول
```
Hex: #003D5B
RGB: (0, 61, 91)
الاستخدام: 
  • العناوين الرئيسية (Headers)
  • شريط التنقل العلوي (Top Navigation)
  • الأزرار الرئيسية (Primary Buttons)
  • الأيقونات المهمة
```

#### Sky Blue - اللون الأساسي الثاني
```
Hex: #30638E
RGB: (48, 99, 142)
الاستخدام:
  • الأزرار الثانوية
  • الروابط التفاعلية
  • Hover States
  • Selected Items
```

---

### الألوان الثانوية (Secondary Colors)

#### Aqua Green - لون التمييز الأول
```
Hex: #00798C
RGB: (0, 121, 140)
الاستخدام:
  • بطاقات الإحصائيات الإيجابية
  • مؤشرات النجاح
  • Active States
  • Progress Indicators
```

#### Warm Orange - لون التمييز الثاني
```
Hex: #EDAE49
RGB: (237, 174, 73)
الاستخدام:
  • Highlights
  • Call-to-Action Elements
  • مؤشرات التنبيه الخفيفة
  • Accent Colors
```

---

### الألوان المحايدة (Neutral Colors)

```
Light Gray:   #F5F5F5  (الخلفيات)
Medium Gray:  #D1D1D1  (الحدود الخفيفة)
Dark Gray:    #555555  (النصوص الثانوية)
Pure White:   #FFFFFF  (الخلفية النقية)
Pure Black:   #000000  (النصوص الرئيسية - استخدام محدود)
```

---

### ألوان الحالة (Status Colors)

```
Success (نجاح):   #4CAF50  (أخضر)
Error (خطأ):      #F44336  (أحمر)
Warning (تحذير):  #FF9800  (برتقالي)
Info (معلومات):  #2196F3  (أزرق)
```

---

## 📝 الخطوط (Typography)

### خط Cairo (الافتراضي)
```
الخط الأساسي: Cairo Regular
الأوزان المتاحة:
  • Cairo Light (300)
  • Cairo Regular (400)
  • Cairo Medium (500)
  • Cairo SemiBold (600)
  • Cairo Bold (700)
  • Cairo ExtraBold (800)
```

### أحجام الخطوط (Font Sizes)

```xaml
<!-- Mega Title -->
<TextBlock FontSize="36" FontWeight="Bold"/>

<!-- Page Title -->
<TextBlock FontSize="28" FontWeight="Bold"/>

<!-- Section Title -->
<TextBlock FontSize="22" FontWeight="SemiBold"/>

<!-- Subtitle -->
<TextBlock FontSize="18" FontWeight="Medium"/>

<!-- Body Large -->
<TextBlock FontSize="16" FontWeight="Regular"/>

<!-- Body (Default) -->
<TextBlock FontSize="14" FontWeight="Regular"/>

<!-- Body Small -->
<TextBlock FontSize="12" FontWeight="Regular"/>

<!-- Caption -->
<TextBlock FontSize="11" FontWeight="Regular"/>
```

---

## 📐 المسافات والأحجام (Spacing & Sizing)

### نظام الشبكة (Grid System)
```
Base Unit: 8px
Spacing Scale:
  • XS:  4px  (0.5 × Base)
  • SM:  8px  (1 × Base)
  • MD:  16px (2 × Base)
  • LG:  24px (3 × Base)
  • XL:  32px (4 × Base)
  • XXL: 48px (6 × Base)
```

### الهوامش القياسية (Standard Margins)
```xaml
<!-- Page Margins -->
<Thickness>20</Thickness>

<!-- Card Padding -->
<Thickness>16</Thickness>

<!-- Section Spacing -->
<Thickness>0,0,0,24</Thickness>

<!-- Button Padding -->
<Thickness>16,8,16,8</Thickness>
```

### أحجام المكونات (Component Sizes)

#### الأزرار (Buttons)
```
Small:   Height="28"  FontSize="12"
Medium:  Height="36"  FontSize="14"
Large:   Height="44"  FontSize="16"
```

#### حقول الإدخال (Text Inputs)
```
Height="36"
FontSize="14"
Padding="12,8"
```

#### البطاقات (Cards)
```
MinWidth="250"
MinHeight="120"
Padding="16"
```

---

## 🧩 المكونات (Components)

### 1. البطاقات (Cards)

#### بطاقة إحصائيات (Stat Card)
```xaml
<materialDesign:Card Width="280" Height="150" Margin="10">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Title -->
        <TextBlock Grid.Row="0" 
                   Text="عنوان البطاقة"
                   FontSize="14"
                   FontWeight="Medium"
                   Foreground="{StaticResource TextLightBrush}"
                   Margin="16,16,16,8"/>
        
        <!-- Value -->
        <TextBlock Grid.Row="1"
                   Text="1,234"
                   FontSize="36"
                   FontWeight="Bold"
                   Foreground="{StaticResource PrimaryDeepBlueBrush}"
                   VerticalAlignment="Center"
                   Margin="16,0"/>
        
        <!-- Subtitle -->
        <TextBlock Grid.Row="2"
                   Text="معلومات إضافية"
                   FontSize="12"
                   Foreground="{StaticResource TextLightBrush}"
                   Margin="16,8,16,16"/>
    </Grid>
</materialDesign:Card>
```

---

### 2. الأزرار (Buttons)

#### Primary Button
```xaml
<Button Style="{StaticResource MaterialDesignRaisedButton}"
        Height="36"
        Padding="16,8"
        FontSize="14"
        FontWeight="Medium"
        Background="{StaticResource PrimaryDeepBlueBrush}"
        BorderBrush="{StaticResource PrimaryDeepBlueBrush}">
    <StackPanel Orientation="Horizontal">
        <materialDesign:PackIcon Kind="ContentSave" 
                                VerticalAlignment="Center"
                                Margin="0,0,8,0"/>
        <TextBlock Text="حفظ" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

#### Secondary Button
```xaml
<Button Style="{StaticResource MaterialDesignOutlinedButton}"
        Height="36"
        Padding="16,8"
        FontSize="14"
        BorderBrush="{StaticResource PrimaryDeepBlueBrush}"
        Foreground="{StaticResource PrimaryDeepBlueBrush}">
    <StackPanel Orientation="Horizontal">
        <materialDesign:PackIcon Kind="Close" 
                                VerticalAlignment="Center"
                                Margin="0,0,8,0"/>
        <TextBlock Text="إلغاء" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

#### Icon Button
```xaml
<Button Style="{StaticResource MaterialDesignIconButton}"
        Width="36"
        Height="36"
        ToolTip="تحديث">
    <materialDesign:PackIcon Kind="Refresh" 
                            Width="20" 
                            Height="20"/>
</Button>
```

---

### 3. حقول الإدخال (Text Inputs)

#### Standard TextBox
```xaml
<TextBox Style="{StaticResource MaterialDesignOutlinedTextBox}"
         materialDesign:HintAssist.Hint="اسم الحقل"
         FontSize="14"
         Height="36"
         Margin="0,0,0,16"
         FlowDirection="RightToLeft"/>
```

#### Password Box
```xaml
<PasswordBox Style="{StaticResource MaterialDesignOutlinedPasswordBox}"
             materialDesign:HintAssist.Hint="كلمة المرور"
             FontSize="14"
             Height="36"
             FlowDirection="RightToLeft"/>
```

#### ComboBox (Dropdown)
```xaml
<ComboBox Style="{StaticResource MaterialDesignOutlinedComboBox}"
          materialDesign:HintAssist.Hint="اختر من القائمة"
          FontSize="14"
          Height="36"
          FlowDirection="RightToLeft">
    <ComboBoxItem Content="خيار 1"/>
    <ComboBoxItem Content="خيار 2"/>
</ComboBox>
```

---

### 4. جداول البيانات (DataGrids)

```xaml
<DataGrid ItemsSource="{Binding Items}"
          AutoGenerateColumns="False"
          CanUserAddRows="False"
          CanUserDeleteRows="False"
          IsReadOnly="True"
          SelectionMode="Single"
          FlowDirection="RightToLeft"
          FontFamily="Cairo"
          FontSize="14"
          RowHeight="40"
          ColumnHeaderHeight="48"
          GridLinesVisibility="Horizontal"
          HeadersVisibility="Column"
          Background="{StaticResource BackgroundWhiteBrush}"
          BorderThickness="1"
          BorderBrush="{StaticResource BorderLightBrush}">
    
    <!-- Column Header Style -->
    <DataGrid.ColumnHeaderStyle>
        <Style TargetType="DataGridColumnHeader" 
               BasedOn="{StaticResource MaterialDesignDataGridColumnHeader}">
            <Setter Property="Background" Value="{StaticResource PrimaryDeepBlueBrush}"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="Padding" Value="16,12"/>
            <Setter Property="HorizontalContentAlignment" Value="Right"/>
        </Style>
    </DataGrid.ColumnHeaderStyle>
    
    <!-- Row Style -->
    <DataGrid.RowStyle>
        <Style TargetType="DataGridRow" 
               BasedOn="{StaticResource MaterialDesignDataGridRow}">
            <Setter Property="Height" Value="40"/>
            <Style.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter Property="Background" Value="{StaticResource NeutralLightGrayBrush}"/>
                </Trigger>
                <Trigger Property="IsSelected" Value="True">
                    <Setter Property="Background" Value="{StaticResource SecondarySkyBlueBrush}"/>
                </Trigger>
            </Style.Triggers>
        </Style>
    </DataGrid.RowStyle>
    
    <!-- Columns -->
    <DataGrid.Columns>
        <DataGridTextColumn Header="الرقم" 
                           Binding="{Binding Id}" 
                           Width="80"/>
        <DataGridTextColumn Header="الاسم" 
                           Binding="{Binding Name}" 
                           Width="*"/>
        <DataGridTextColumn Header="التاريخ" 
                           Binding="{Binding Date, StringFormat=yyyy/MM/dd}" 
                           Width="120"/>
    </DataGrid.Columns>
</DataGrid>
```

---

### 5. Dialogs و MessageBoxes

```xaml
<!-- Dialog Host في MainWindow -->
<materialDesign:DialogHost Identifier="RootDialog"
                          CloseOnClickAway="True">
    <materialDesign:DialogHost.DialogContent>
        <StackPanel Margin="20" MinWidth="300">
            <TextBlock Text="عنوان الرسالة"
                      FontSize="18"
                      FontWeight="SemiBold"
                      Margin="0,0,0,16"/>
            
            <TextBlock Text="محتوى الرسالة"
                      FontSize="14"
                      TextWrapping="Wrap"
                      Margin="0,0,0,20"/>
            
            <StackPanel Orientation="Horizontal" 
                       HorizontalAlignment="Left"
                       FlowDirection="LeftToRight">
                <Button Content="موافق"
                       Style="{StaticResource MaterialDesignRaisedButton}"
                       Width="80"
                       Margin="0,0,8,0"
                       Command="{x:Static materialDesign:DialogHost.CloseDialogCommand}">
                    <Button.CommandParameter>
                        <system:Boolean>True</system:Boolean>
                    </Button.CommandParameter>
                </Button>
                
                <Button Content="إلغاء"
                       Style="{StaticResource MaterialDesignOutlinedButton}"
                       Width="80"
                       Command="{x:Static materialDesign:DialogHost.CloseDialogCommand}">
                    <Button.CommandParameter>
                        <system:Boolean>False</system:Boolean>
                    </Button.CommandParameter>
                </Button>
            </StackPanel>
        </StackPanel>
    </materialDesign:DialogHost.DialogContent>
</materialDesign:DialogHost>
```

---

## 🎭 الحركات والانتقالات (Animations & Transitions)

### Page Transitions
```xaml
<Page.Resources>
    <Storyboard x:Key="PageLoadAnimation">
        <DoubleAnimation Storyboard.TargetProperty="Opacity"
                        From="0" To="1"
                        Duration="0:0:0.3"
                        EasingFunction="{StaticResource EaseOut}"/>
        
        <ThicknessAnimation Storyboard.TargetProperty="Margin"
                           From="20,0,0,0" To="0"
                           Duration="0:0:0.3"
                           EasingFunction="{StaticResource EaseOut}"/>
    </Storyboard>
</Page.Resources>

<Page.Triggers>
    <EventTrigger RoutedEvent="Loaded">
        <BeginStoryboard Storyboard="{StaticResource PageLoadAnimation}"/>
    </EventTrigger>
</Page.Triggers>
```

### Button Hover Effect
```xaml
<Button.Style>
    <Style TargetType="Button" BasedOn="{StaticResource MaterialDesignRaisedButton}">
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Trigger.EnterActions>
                    <BeginStoryboard>
                        <Storyboard>
                            <DoubleAnimation Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleX)"
                                           To="1.05"
                                           Duration="0:0:0.15"/>
                            <DoubleAnimation Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleY)"
                                           To="1.05"
                                           Duration="0:0:0.15"/>
                        </Storyboard>
                    </BeginStoryboard>
                </Trigger.EnterActions>
                <Trigger.ExitActions>
                    <BeginStoryboard>
                        <Storyboard>
                            <DoubleAnimation Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleX)"
                                           To="1"
                                           Duration="0:0:0.15"/>
                            <DoubleAnimation Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleY)"
                                           To="1"
                                           Duration="0:0:0.15"/>
                        </Storyboard>
                    </BeginStoryboard>
                </Trigger.ExitActions>
            </Trigger>
        </Style.Triggers>
    </Style>
</Button.Style>
```

---

## 📱 التجاوب (Responsiveness)

### Adaptive Layouts
```xaml
<Grid>
    <Grid.ColumnDefinitions>
        <!-- تتكيف حسب حجم الشاشة -->
        <ColumnDefinition Width="Auto" MinWidth="250"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    
    <!-- Sidebar -->
    <StackPanel Grid.Column="0" Background="{StaticResource BackgroundCardBrush}">
        <!-- Navigation Items -->
    </StackPanel>
    
    <!-- Main Content -->
    <ScrollViewer Grid.Column="1" VerticalScrollBarVisibility="Auto">
        <!-- Page Content -->
    </ScrollViewer>
</Grid>
```

---

## ✅ قائمة التحقق (Design Checklist)

### عند تصميم صفحة جديدة
```
□ استخدام Cairo Font
□ تطبيق RTL (FlowDirection="RightToLeft")
□ الألوان من نظام الألوان المعتمد
□ المسافات من نظام الشبكة (8px base)
□ أحجام الخطوط من المقاسات المحددة
□ الأزرار لها حالات Hover و Disabled واضحة
□ حقول الإدخال لها Validation واضح
□ Accessibility: TabIndex صحيح
□ Accessibility: Keyboard Navigation يعمل
□ Loading States موجودة
□ Error States موجودة
□ Empty States موجودة
```

---

## 🎨 أمثلة تطبيقية

### مثال: صفحة Dashboard كاملة
[سيتم إضافة مثال كامل في ملف منفصل]

### مثال: نموذج إدخال بيانات
[سيتم إضافة مثال كامل في ملف منفصل]

### مثال: تقرير مع جدول بيانات
[سيتم إضافة مثال كامل في ملف منفصل]

---

**آخر تحديث**: 14 أكتوبر 2025  
**الحالة**: مرجع معتمد ✅  
**الإصدار**: 1.0



