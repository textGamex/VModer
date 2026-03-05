# VModer

为 Hearts of iron IV (钢铁雄心4) 提供 CWTools 缺失的功能, 例如实时显示 Modifier 效果.

Language: 简体中文 | [English](https://github.com/textGamex/VModer/blob/main/README.md)

---

开发了一个新的国策树可视化编辑器, 欢迎体验, 下载链接:
[安装版](https://packages.paradoxtarget.top/ParadoxArt-win-x64-stable-Setup.exe) | [便携版](https://packages.paradoxtarget.top/ParadoxArt-win-x64-stable-Portable.zip)

## 赞助

如果您觉得此扩展帮助了您, 可以通过爱发电请我喝一杯奶茶, 这有助于我持续开发此扩展

[爱发电(ID: textGamex)](https://afdian.com/a/textGamex)

---

![license](https://img.shields.io/github/license/textGamex/Vmoder?style=for-the-badge&color=blue)
[![star](https://img.shields.io/github/stars/textGamex/vmoder?style=for-the-badge)](https://github.com/textGamex/VModer)
![language](https://img.shields.io/badge/Language-CSharp-blue?style=for-the-badge)

[问题反馈/功能请求](https://github.com/textGamex/VModer/issues/new)

---

## 使用指南

为启动扩展, 您需要确保您的语言模式为`hoi4`, [`CWTools`](https://marketplace.visualstudio.com/items?itemName=tboby.cwtools-vscode)
或[`Paradox Highlight`](https://marketplace.visualstudio.com/items?itemName=dragon-archer.paradox-highlight)扩展提供此语言模式,

您也可以选择安装其他提供`hoi4`语言模式的扩展.

此外, 您还需要确保打开了工作区.

---

## 功能

### Modifier

支持所有 `modifier` 和 `modifiers` 语句的可视化和`common\modifiers`文件夹下的文件可视化.

### Character

特质效果, 特质Icon, 内阁职位, 所属意识形态

特质效果可显示特质块和单独特质效果的显示

### Technology (科技)

可视化 **Technology** 修饰符, 像 **Modifier** 一样, 支持节点和单一修饰符的显示

显示 Technology 本地化名称

### 颜色选择器

支持国家颜色, 意识形态颜色定义, 支持 HSV 和 RGB 转换

自动计算应用颜色修饰符后的结果, 实现所见即所得.

### 错误分析

补充部分 CWTools 不能提供的错误分析类型

- 建筑等级分析 (仅 State 文件)
- Character 技能等级分析 (仅 Character 文件夹下)

### 特质预览器

显示游戏和 MOD 中所有定义的特质和定义的 Icon, 并可以按来源(游戏, MOD), 特质类型筛选, 显示特质修饰效果

右键菜单功能

- 可复制特质 ID
- 在文件中打开此特质

### 修饰符查询器

显示游戏内可用的修饰符, 并可以按名称, 类别筛选


### 国家定义文件

显示`set_technology`内科技的本地化值

## 功能展示

### Modifier 可视化

> Modifier 节点
>
>![ModifierNodeImg](https://www.helloimg.com/i/2025/01/18/678a838fd83d0.png)

>单修饰符词条
>
>![ModifierLeafImg](https://www.helloimg.com/i/2025/01/18/678a838fdb9e0.png)

### Character 内容可视化

>将领
>
>![General](https://www.helloimg.com/i/2025/01/18/678a83903e74b.png)

>内阁
>
>![image](https://www.helloimg.com/i/2025/01/18/678a8390052ea.png)

> Character 总览
>
>![image](https://www.helloimg.com/i/2025/01/18/678a83910610a.png)

### 修饰符查询器

![Modifier](https://free4.yunpng.top/2025/04/14/67fd22b807f94.png)

### 特质预览器

![trait](https://www.helloimg.com/i/2025/03/12/67d18f36cdfaf.png)

### 颜色选择器

![ColorPicker](https://www.helloimg.com/i/2025/01/18/678b34fe8e854.png)
