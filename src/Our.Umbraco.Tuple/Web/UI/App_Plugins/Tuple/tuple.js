angular.module("umbraco").controller("Our.Umbraco.Tuple.DataTypePickerController", [
    "$scope",
    "dataTypeHelper",
    "dataTypeResource",
    "entityResource",
    "Our.Umbraco.Tuple.Resources",
    function ($scope, dataTypeHelper, dataTypeResource, entityResource, tupleResource) {

        const maxItems = 8;

        $scope.model.value = $scope.model.value || [];

        var vm = this;

        vm.selectedDataTypes = [];
        vm.allowAdd = $scope.model.value.length < maxItems;
        vm.allowRemove = true;
        vm.allowOpen = true;
        vm.allowEdit = true;
        vm.sortable = true;

        vm.sortableOptions = {
            axis: "y",
            containment: "parent",
            cursor: "move",
            opacity: 0.7,
            scroll: true,
            tolerance: "pointer",
            stop: function (e, ui) {

                var uids = vm.selectedDataTypes.map(function (item) {
                    return item.uid;
                });

                $scope.model.value.sort(function (a, b) {
                    return uids.indexOf(a.key) - uids.indexOf(b.key);
                });

                setDirty();
            }
        };

        vm.add = add;
        vm.edit = edit;
        vm.open = open;
        vm.remove = remove;

        if ($scope.model.value.length > 0) {
            var guids = $scope.model.value.map(function (item) {
                return item.dtd;
            });

            tupleResource.getDataTypesByKey(guids).then(function (dataTypes) {
                _.each($scope.model.value, function (item, index) {
                    var dataType = _.find(dataTypes, function (d) {
                        return d.key === item.dtd;
                    });
                    vm.selectedDataTypes.push(angular.extend({ uid: item.key }, dataType));
                });
            });
        }

        function add() {
            open(-1, {}); // might seem odd, but it's the intended behaviour
        };

        function edit(index, item) {
            dataTypeResource.getById(item.id).then(function (dataType) {

                vm.editorSettingsOverlay = {};
                vm.editorSettingsOverlay.title = "Editor settings";
                vm.editorSettingsOverlay.view = "views/common/overlays/contenttypeeditor/editorsettings/editorsettings.html";
                vm.editorSettingsOverlay.dataType = dataType;
                vm.editorSettingsOverlay.show = true;

                vm.editorSettingsOverlay.submit = function (model) {
                    var preValues = dataTypeHelper.createPreValueProps(model.dataType.preValues);
                    dataTypeResource.save(model.dataType, preValues, false).then(function (newDataType) {
                        vm.selectedDataTypes[index].name = newDataType.name;
                        vm.editorSettingsOverlay.show = false;
                        vm.editorSettingsOverlay = null;
                    });
                };

                vm.editorSettingsOverlay.close = function (oldModel) {
                    vm.editorSettingsOverlay.show = false;
                    vm.editorSettingsOverlay = null;
                };

            });
        };

        function open(index, item) {
            vm.editorPickerOverlay = {
                property: item,
                view: "views/common/overlays/contenttypeeditor/editorpicker/editorpicker.html",
                show: true
            };

            vm.editorPickerOverlay.submit = function (model) {
                setModelValue(model.property, index);

                vm.editorPickerOverlay.show = false;
                vm.editorPickerOverlay = null;
            };

            vm.editorPickerOverlay.close = function (model) {
                vm.editorPickerOverlay.show = false;
                vm.editorPickerOverlay = null;
            };
        };

        function remove(index) {
            $scope.model.value.splice(index, 1);
            vm.selectedDataTypes.splice(index, 1);

            vm.allowAdd = $scope.model.value.length < maxItems;

            setDirty();
        };

        function setModelValue(property, index) {
            entityResource.getById(property.dataTypeId, "DataType").then(function (entity) {

                var dataType = {
                    id: entity.id,
                    key: entity.key,
                    name: entity.name,
                    icon: property.dataTypeIcon,
                    description: property.editor
                };

                if (index === -1) {

                    dataType.uid = generateUid();

                    $scope.model.value.push({ key: dataType.uid, dtd: entity.key });
                    vm.selectedDataTypes.push(dataType);

                    vm.allowAdd = $scope.model.value.length < maxItems;

                } else {

                    dataType.uid = $scope.model.value[index].key;

                    $scope.model.value[index].dtd = entity.key;
                    vm.selectedDataTypes[index] = dataType;

                }
            });
        };

        function setDirty() {
            if ($scope.propertyForm) {
                $scope.propertyForm.$setDirty();
            }
        };

        var lut = []; for (var i = 0; i < 256; i++) { lut[i] = (i < 16 ? '0' : '') + (i).toString(16); }
        function generateUid() {
            var d0 = Math.random() * 0xffffffff | 0;
            var d1 = Math.random() * 0xffffffff | 0;
            var d2 = Math.random() * 0xffffffff | 0;
            var d3 = Math.random() * 0xffffffff | 0;
            return lut[d0 & 0xff] + lut[d0 >> 8 & 0xff] + lut[d0 >> 16 & 0xff] + lut[d0 >> 24 & 0xff] + '-' +
                lut[d1 & 0xff] + lut[d1 >> 8 & 0xff] + '-' + lut[d1 >> 16 & 0x0f | 0x40] + lut[d1 >> 24 & 0xff] + '-' +
                lut[d2 & 0x3f | 0x80] + lut[d2 >> 8 & 0xff] + '-' + lut[d2 >> 16 & 0xff] + lut[d2 >> 24 & 0xff] +
                lut[d3 & 0xff] + lut[d3 >> 8 & 0xff] + lut[d3 >> 16 & 0xff] + lut[d3 >> 24 & 0xff];
        };
    }
]);

angular.module("umbraco").controller("Our.Umbraco.Tuple.EditorController", [
    "$scope",
    "umbPropEditorHelper",
    "Our.Umbraco.Tuple.Resources",
    function ($scope, umbPropEditorHelper, tupleResource) {

        //console.log("init", $scope.model.config.dataTypes, $scope.model.value);

        // take a copy of the config data
        var config = JSON.parse(JSON.stringify($scope.model.config.dataTypes));

        if (!($scope.model.value instanceof Array)) {
            $scope.model.value = JSON.parse(JSON.stringify(config));
        }

        var vm = this;

        vm.controls = [];

        vm.className = "span5";

        if (config.length === 1) {
            vm.className = "span12";
        } else if (config.length === 2) {
            vm.className = "span6";
        } else if (config.length === 3) {
            vm.className = "span4";
        } else if (config.length === 4) {
            vm.className = "span3";
        } else if (config.length === 5 || config.length === 6) {
            vm.className = "span2";
        } else if (config.length === 7 || config.length === 8) {
            vm.className = "span1";
        }

        var dataTypeGuids = _.uniq(config.map(function (item) {
            return item.dtd;
        }));

        // TODO: Could we cache the results? As a Tuple being used in a PropertyList will make a lot of WebAPI requests.
        tupleResource.getPropertyTypeScaffoldsByKey(dataTypeGuids).then(function (scaffolds) {

            _.each(config, function (item, index) {

                var valueItem = _.find($scope.model.value, function (v) {
                    return v.key === item.key;
                });

                var propertyType = _.find(scaffolds, function (s) {
                    return s.dataTypeGuid === item.dtd;
                });

                // NOTE: Must be a copy of the config, not the same object reference.
                // Otherwise any config modifications made by the editor will apply to following editors.
                var propertyTypeConfig = JSON.parse(JSON.stringify(propertyType.config));
                var propertyTypeViewPath = umbPropEditorHelper.getViewPath(propertyType.view);

                vm.controls.push({
                    alias: $scope.model.alias + "_item" + index,
                    config: propertyTypeConfig,
                    key: item.key,
                    view: propertyTypeViewPath,
                    value: !!valueItem ? valueItem.value : null
                });
            });

        });

        function setDirty() {
            if ($scope.propertyForm) {
                $scope.propertyForm.$setDirty();
            }
        };

        var unsubscribe = $scope.$on("formSubmitting", function (ev, args) {

            $scope.$broadcast("tupleFormSubmitting");

            var tmpValues = JSON.parse(JSON.stringify(config));

            _.each(vm.controls, function (control, index) {
                tmpValues[index].value = control.value;
            });

            $scope.model.value = tmpValues;
        });

        $scope.$on("$destroy", function () {
            unsubscribe();
        });
    }
]);

angular.module("umbraco.directives").directive("tuplePropertyEditor", [
    function () {

        var link = function ($scope, $element, $attrs, $ctrl) {

            var unsubscribe = $scope.$on("tupleFormSubmitting", function (ev, args) {
                $scope.$broadcast("formSubmitting", { scope: $scope });
            });

            $scope.$on("$destroy", function () {
                unsubscribe();
            });
        };

        return {
            require: "^form",
            restrict: "E",
            rep1ace: true,
            link: link,
            template: '<umb-property-editor model="control" />',
            scope: {
                control: "=model"
            }
        };
    }
]);

angular.module("umbraco.resources").factory("Our.Umbraco.Tuple.Resources",
    function ($http, umbRequestHelper) {
        return {
            getDataTypesByKey: function (keys) {
                return umbRequestHelper.resourcePromise(
                    $http({
                        url: "/umbraco/backoffice/Tuple/TupleApi/GetDataTypesByKey",
                        method: "GET",
                        params: { keys: keys }
                    }),
                    "Failed to retrieve datatypes by key"
                );
            },
            getPropertyTypeScaffoldsByKey: function (keys) {
                return umbRequestHelper.resourcePromise(
                    $http({
                        url: "/umbraco/backoffice/Tuple/TupleApi/GetPropertyTypeScaffoldsByKey",
                        method: "GET",
                        params: { keys: keys }
                    }),
                    "Failed to retrieve property type scaffolds by key"
                );
            }
        };
    });
