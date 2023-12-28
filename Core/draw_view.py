
#coding:utf-8

import os;
import sys

import matplotlib.font_manager
import matplotlib.pyplot as plt
import matplotlib.font_manager as mfm
from matplotlib import style
import numpy as np
import plotly.offline

style.use('ggplot')

print(sys.platform)
if sys.platform.__contains__("win"):
   plt.rcParams['font.sans-serif'] = ['SimHei']

# plt.rcParams['font.sans-serif'] = ['SimHei', 'Songti SC', 'STFangsong']
plt.rcParams['axes.unicode_minus'] = False

workSpace = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))

resPath = os.path.join(workSpace, "res")
logPath = os.path.join(workSpace, "logs")
jarFile = os.path.join(workSpace, "MakeGraph.jar")
plt.rcParams['xtick.labelsize'] = 8
bbox = dict(boxstyle="round", fc="0.8")
arrowprops = dict(
    arrowstyle="->",
    connectionstyle="angle, angleA = 0, \
    angleB = 90, rad = 10")

HSpace = 0.63

def get_value_str(unit_conversion,value):
    if unit_conversion == 0:
        mb = value * 1.0 / 1024 / 1024
        return "%s" % round(mb, 2)
    if  unit_conversion == 1:
        mb = value * 1.0 / 1024
        return "%s" % round(mb, 2)
    if unit_conversion == 3:
        count = value * 0.001
        return "%s" % round(count, 2)
    return value

OneMB =  1024 * 1024
OneKB =  1024
def get_value_show_str(unit_conversion,value):
    if unit_conversion == 0:
        if value>= OneMB:
            mb = value * 1.0 / OneMB
            return "%s MB" % round(mb, 2)
        else:
            mb = value * 1.0 / OneKB
            return "%s KB" % round(mb, 2)

    if  unit_conversion == 1:
        mb = value * 1.0 / 1024
        return "%s" % round(mb, 2)
    if unit_conversion == 3:
        count = value * 0.001
        return "%s" % round(count, 2)
    return value


def get_max_value_str(unit_conversion,max_value):
    if unit_conversion == 0:
        mb = max_value * 1.0 / 1024 / 1024
        return "%s" % round(mb, 2)
    if unit_conversion == 1:
        mb = max_value * 1.0 / 1024
        return "%s" % round(mb, 2)
    if unit_conversion == 2:
        return "%s" % max_value
    if unit_conversion == 3:
        count = max_value * 0.001
        return "%s" % round(count, 2)
    return "%s" % max_value


def get_label_name(unit_conversion,name):
    if unit_conversion == 0:
        return "%s(MB)" % name
    if unit_conversion == 1:
        return "%s(MB)" % name
    if unit_conversion == 2:
        return "%s(Unit)" % name
    if unit_conversion == 3:
        return "%s(K)" % name


class DrawInfo:
    def __init__(self, line):
        info = line.replace("\n", "").split(",")
        self.category = info[0]
        self.name = info[1]
        self.des = info[2]
        self.unitConversion = int(info[3])
        self.maxValue = int(info[4])
        self.value = int(info[5])
        if self.unitConversion == 0:
            self.show_value = self.value * 1.0 / 1024 / 1024
            self.show_max_value = self.maxValue * 1.0 / 1024 / 1024
        elif self.unitConversion == 1:
            self.show_value = self.value * 1.0 / 1024
            self.show_max_value = self.maxValue * 1.0 / 1024
        elif self.unitConversion == 3:
            self.show_value = self.value * 0.001
            self.show_max_value = self.maxValue * 0.001
        else:
            self.show_value = self.value
            self.show_max_value = self.maxValue

    def get_value_str(self):
        return get_value_str(unit_conversion=self.unitConversion,value=self.value)

    def get_max_value_str(self):
        return get_max_value_str(unit_conversion=self.unitConversion,max_value=self.maxValue)

    def get_label_name(self):
        return get_label_name(unit_conversion=self.unitConversion,name=self.name)

    def is_out_limit(self):
        return self.value > self.maxValue


class TopInfo:
    def __init__(self,line):
        info = line.replace("\n", "").split(",")
        self.category = info[1]
        self.des = info[2]
        self.unitConversion = int(info[3])
        self.maxValue = int(info[4])
        self.value = []
        self.parse_values(info[5])

    def parse_values(self,value):
        line = value[1:-1]
        items = line.split("$")
        for item in items:
            if len(item) > 0:
               v = item.split("=")
               self.value.append((v[0],int(v[1])))
        print(self.value)

class Report:
    def __init__(self, sys_info, content,top_info):
        self.sys_info = sys_info
        self.content = content
        self.top_info = top_info

    def export_report(self, out_path):
        infos = dict()
        for draw_info in self.content:
            items = infos.get(draw_info.category)
            if items is None:
                items = []
                infos.setdefault(draw_info.category, items)
            items.append(draw_info)
        self.infos = infos
        draw_category_view(self, out_path)


def check_line_invalid(line):
    items = line.replace("\n", "").split(",")
    return len(items) == 6

def check_top_line_invalid(line):
    return check_line_invalid(line)


def check_line_has_value(line):
    return len(line.strip()) > 0


def parse_file_infos(file_path):
    draw_infos = []
    sys_info = []
    top_info = []
    with open(file_path, 'r', encoding= "utf-8") as file:
        lines = file.readlines()
        length = len(lines)
        for i in range(0, length):
            line = lines[i]
            if line.startswith("---["):
                title = line.strip().replace("---[", "").replace("]", "")
                if title == "Data":
                    while i < (length - 1):
                        i = i + 1
                        line = lines[i]
                        if line.startswith("---["):
                            i = i - 1
                            break
                        if check_line_invalid(line):
                            draw_info = DrawInfo(line)
                            draw_infos.append(draw_info)
                        else:
                            print("Jump empty line")
                elif title == "SystemInfo":
                    while i < (length - 1):
                        i = i + 1
                        line = lines[i]
                        if line.startswith("---["):
                            i = i - 1
                            break
                        if check_line_has_value(line):
                            sys_info.append(line)
                elif title == "Top":
                    while i < (length - 1):
                        i = i + 1
                        line = lines[i]
                        if line.startswith("---["):
                            i = i - 1
                            break
                        if check_top_line_invalid(line):
                            top_info.append(TopInfo(line))
                        else:
                            print("Jump empty line")

    report = Report(sys_info=sys_info, content=draw_infos,top_info=top_info)
    return report


def draw_item_view(ax, category, draw_infos):
    ax.set_title(category)
    length = len(draw_infos)
    names = []
    values = []
    max_values = []
    width = 0.2
    x_ticks = []
    max_length = 10
    color_green = []
    color = []

    for i in range(0, max_length):
        names.append("-")
        values.append(0)
        max_values.append(0)
        color_green.append("yellow")
        color.append("g")

    for i in range(0, length):
        names[i] = "%s" % draw_infos[i].get_label_name()
        values[i] = draw_infos[i].show_value
        max_values[i] = draw_infos[i].show_max_value
        if draw_infos[i].is_out_limit():
            color[i] = "r"

    for i in range(0, max_length):
        x_ticks.append(i * width + width)
    ax.set_xticks(x_ticks)
    # ax.set_xticklabels(names)
    ax.legend([])
    plt.sca(ax)
    plt.xticks(np.arange(10), labels=names)
    plt.bar(np.arange(10) - 0.1, values, width=0.2, color=color)
    plt.bar(np.arange(10) + 0.1, max_values, width=0.2, color=color_green)

    ax.grid(False)
    for i in range(0, length):
        plt.text(i - 0.1, values[i] + 1, draw_infos[i].get_value_str(), va="bottom", ha='center', fontsize=8)
        plt.text(i + 0.1, max_values[i] + 1, draw_infos[i].get_max_value_str(), va="bottom", ha='center', fontsize=6, color="b")


def draw_sys_info(sys_info_ax,report):
    plt.sca(sys_info_ax)
    sys_info_ax.grid(False)
    sys_info_ax.set_facecolor((0.9,0.9,0.9,1))
    sys_info_ax.xaxis.set_visible(False)
    sys_info_ax.yaxis.set_visible(False)
    h = HSpace - 0.15
    for i in range(0,len(report.sys_info)):
        info = report.sys_info[i]
        plt.text(0.01, h, info, va="bottom", ha='left', fontsize=12)
        h = h - 0.15


def draw_top_info(ax,top_info : TopInfo):
    ax.set_title("Top 10 %s " % top_info.category)
    ax.grid(False)
    plt.sca(ax)

    values = []
    color = []
    names = []
    labels = []
    max_length = 10
    for i in range(0, max_length):
        values.append(0)
        names.append('-')
        labels.append("Top %s"% (i+1))
        color.append("r")

    for i in range(0, len(top_info.value)):
        names[i] = top_info.value[i][0]
        values[i] = top_info.value[i][1]

    values.reverse()
    names.reverse()
    color.reverse()
    labels.reverse()

    width = 1
    y_ticks = []
    for i in range(0, max_length):
        y_ticks.append(i * width)
    plt.yticks(np.arange(10), labels=labels,fontsize=6)
    ax.set_yticks(y_ticks)
    ax.legend([])
    plt.sca(ax)
    plt.barh(np.arange(10), values, height=0.6, color=color)
    for i in range(0, len(values)) :
        if names[i] != "-" :
           plt.text(values[i], i,"%s(%s)" % (names[i],get_value_show_str(top_info.unitConversion,values[i])), va="center", ha='left', fontsize=7)


def draw_category_view(report, out_path, show=True):
    infos_dic = report.infos
    count = len(infos_dic) + 1 + len(report.top_info)
    per = count
    height = count * 2.2
    if height < 10:
        height = 10
    fig = plt.figure(figsize=(24, height), dpi=128)
    i = 1
    sys_info = fig.add_subplot(per, 1, i)
    draw_sys_info(sys_info,report)
    i = i + 1
    for key, value in infos_dic.items():
        ax = fig.add_subplot(per, 1, i)
        # ax = fig.add_subplot(per, 1, i,frameon = False,facecolor = (0.9,0.9,0.9,0.7),)
        draw_item_view(ax, key, value)
        i = i + 1

    for top in report.top_info:
        top_info = fig.add_subplot(per, 1, i)
        draw_top_info(top_info, top)
        i = i + 1

    fig.subplots_adjust(wspace=3, hspace=HSpace)
    plt.savefig(out_path)  #
    if show:
        plt.show()




def draw_file(file_path, out_path):
    report = parse_file_infos(file_path)
    report.export_report(out_path)
