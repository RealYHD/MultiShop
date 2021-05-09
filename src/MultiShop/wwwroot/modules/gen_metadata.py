import os
import json

modulepaths = []

for content in os.listdir(os.getcwd()):
    components = os.path.splitext(content)
    if (os.path.isfile(content) and  components[1] == ".dll"):
        print("Adding \"{0}\" to list of modules.".format(components[0]))
        modulepaths.append(components[0])

file = open("modules_content.json", "w")
json.dump(modulepaths, file, sort_keys=True, indent=4)
file.close()