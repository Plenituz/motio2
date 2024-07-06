<![CDATA[
var _createClass = function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; }();

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

var MotioAnimatedSvg = function () {
	function MotioAnimatedSvg() {
		_classCallCheck(this, MotioAnimatedSvg);

		this.frame = 0;
		this.lastFrame = /*LASTFRAME*/;
		this.framerate = /*PLAYBACKFRAMERATE*/;
		this.loop = /*LOOPOPTION*/;
		this.frameMeshGroup = [];
		for (var i = 0; i <= this.lastFrame; i++) {
			this.frameMeshGroup.push(document.getElementById('Motio./*FILENAME*/.frame' + i.toString()));
		}
		this.previousMeshGroup = this.frameMeshGroup[0];
		this.player = setInterval(this.nextFrame.bind(this), 1000.0 / this.framerate);
	}

	_createClass(MotioAnimatedSvg, [{
		key: "nextFrame",
		value: function nextFrame() {
			this.frame++;
			if (this.frame == this.lastFrame) {
				this.update();
				if (this.hasOwnProperty("complete")) this.complete();
				if (this.loop != true) {
					this.pause();
				};
			} else {
				if (this.frame > this.lastFrame) this.frame = 0;
				this.update();
			}
		}
	}, {
		key: "previousFrame",
		value: function previousFrame() {
			this.frame--;
			if (this.frame == 0) {
				this.update();
				if (this.hasOwnProperty("complete")) this.complete();
				if (this.loop != true) {
					this.pause();
				};
			} else {
				if (this.frame < 0) this.frame = this.lastFrame;
				this.update();
			}
		}
	}, {
		key: "update",
		value: function update() {
			var currentMeshGroup = this.frameMeshGroup[this.frame];
			this.previousMeshGroup.setAttribute('display', 'none');
			currentMeshGroup.setAttribute('display', 'inline');
			this.previousMeshGroup = currentMeshGroup;
			if (this.hasOwnProperty("frameUpdate")) this.frameUpdate();
		}
	}, {
		key: "play",
		value: function play() {
			var framerate = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : undefined;

			if (!isNaN(framerate)) this.framerate = framerate;
			this.pause();
			this.player = setInterval(this.nextFrame.bind(this), 1000.0 / this.framerate);
		}
	}, {
		key: "rewind",
		value: function rewind() {
			var framerate = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : undefined;

			if (!isNaN(framerate)) this.framerate = framerate;
			this.pause();
			this.player = setInterval(this.previousFrame.bind(this), 1000.0 / this.framerate);
		}
	}, {
		key: "pause",
		value: function pause() {
			clearInterval(this.player);
		}
	}, {
		key: "seekTo",
		value: function seekTo(frameNumber) {
			this.frame = frameNumber;
			this.update();
		}
	}, {
		key: "onComplete",
		value: function onComplete(eventFunction) {
			this.complete = eventFunction;
		}
	}, {
		key: "onFrameUpdate",
		value: function onFrameUpdate(eventFunction) {
			this.frameUpdate = eventFunction;
		}
	}]);

	return MotioAnimatedSvg;
}();

(function () {
	var container = window.parent || window;
	container.Motio = container.Motio || {};
	container.Motio['/*FILENAME*/'] = new MotioAnimatedSvg();
})();
]]>