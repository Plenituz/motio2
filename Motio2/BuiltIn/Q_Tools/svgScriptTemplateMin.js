<![CDATA[
var _createClass=function(){function e(e,t){for(var a=0;a<t.length;a++){var i=t[a];i.enumerable=i.enumerable||!1,i.configurable=!0,"value"in i&&(i.writable=!0),Object.defineProperty(e,i.key,i)}}return function(t,a,i){return a&&e(t.prototype,a),i&&e(t,i),t}}();function _classCallCheck(e,t){if(!(e instanceof t))throw new TypeError("Cannot call a class as a function")}var MotioAnimatedSvg=function(){function e(){_classCallCheck(this,e),this.frame=0,this.lastFrame=/*LASTFRAME*/,this.framerate=/*PLAYBACKFRAMERATE*/,this.loop=/*LOOPOPTION*/,this.frameMeshGroup=[];for(var t=0;t<=this.lastFrame;t++)this.frameMeshGroup.push(document.getElementById("Motio./*FILENAME*/.frame"+t.toString()));this.previousMeshGroup=this.frameMeshGroup[0],this.player=setInterval(this.nextFrame.bind(this),1e3/this.framerate)}return _createClass(e,[{key:"nextFrame",value:function(){this.frame++,this.frame==this.lastFrame?(this.update(),this.hasOwnProperty("complete")&&this.complete(),1!=this.loop&&this.pause()):(this.frame>this.lastFrame&&(this.frame=0),this.update())}},{key:"previousFrame",value:function(){this.frame--,0==this.frame?(this.update(),this.hasOwnProperty("complete")&&this.complete(),1!=this.loop&&this.pause()):(this.frame<0&&(this.frame=this.lastFrame),this.update())}},{key:"update",value:function(){var e=this.frameMeshGroup[this.frame];this.previousMeshGroup.setAttribute("display","none"),e.setAttribute("display","inline"),this.previousMeshGroup=e,this.hasOwnProperty("frameUpdate")&&this.frameUpdate()}},{key:"play",value:function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:void 0;isNaN(e)||(this.framerate=e),this.pause(),this.player=setInterval(this.nextFrame.bind(this),1e3/this.framerate)}},{key:"rewind",value:function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:void 0;isNaN(e)||(this.framerate=e),this.pause(),this.player=setInterval(this.previousFrame.bind(this),1e3/this.framerate)}},{key:"pause",value:function(){clearInterval(this.player)}},{key:"seekTo",value:function(e){this.frame=e,this.update()}},{key:"onComplete",value:function(e){this.complete=e}},{key:"onFrameUpdate",value:function(e){this.frameUpdate=e}}]),e}();!function(){var e=window.parent||window;e.Motio=e.Motio||{},e.Motio["/*FILENAME*/"]=new MotioAnimatedSvg}();
]]>