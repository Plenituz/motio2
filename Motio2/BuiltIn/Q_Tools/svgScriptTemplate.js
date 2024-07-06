<![CDATA[
class MotioAnimatedSvg{
	constructor(){
		this.frame = 0;
		this.lastFrame = /*LASTFRAME*/;
		this.framerate = /*PLAYBACKFRAMERATE*/;
		this.loop = /*LOOPOPTION*/;
		this.frameMeshGroup = [];
		for (var i = 0; i <= this.lastFrame; i++){
			this.frameMeshGroup.push(document.getElementById('Motio./*FILENAME*/.frame' + i.toString()));
		}
		this.previousMeshGroup = this.frameMeshGroup[0];
		this.player = setInterval(this.nextFrame.bind(this), 1000.0 / this.framerate);
	}

	nextFrame(){
		this.frame++;
		if(this.frame == this.lastFrame){
			this.update();
			if (this.hasOwnProperty("complete")) this.complete();
			if (this.loop != true){this.pause()};
		}else{
			if (this.frame > this.lastFrame) this.frame = 0;
			this.update();
		}
	}
	
	previousFrame(){
		this.frame--;
		if(this.frame == 0){
			this.update();
			if (this.hasOwnProperty("complete")) this.complete();
			if (this.loop != true){this.pause()};
		}else{
			if (this.frame < 0) this.frame = this.lastFrame;
			this.update();
		}
	}
	
	update(){
		var currentMeshGroup = this.frameMeshGroup[this.frame]
		this.previousMeshGroup.setAttribute('display','none');
		currentMeshGroup.setAttribute('display','inline');
		this.previousMeshGroup = currentMeshGroup;
		if (this.hasOwnProperty("frameUpdate")) this.frameUpdate();
	}
	
	play(framerate = undefined){
		if (!isNaN(framerate)) this.framerate = framerate;
		this.pause();
		this.player = setInterval(this.nextFrame.bind(this), 1000.0 / this.framerate); 
	}
	
	rewind(framerate = undefined){
		if (!isNaN(framerate)) this.framerate = framerate;
		this.pause();
		this.player = setInterval(this.previousFrame.bind(this), 1000.0 / this.framerate); 
	}
	
	pause(){
		clearInterval(this.player);
	}
	
	seekTo(frameNumber){
		this.frame = frameNumber;
		this.update();
	}
	
	onComplete(eventFunction){
		this.complete = eventFunction;
	}
	
	onFrameUpdate(eventFunction){
		this.frameUpdate = eventFunction;
	}
	
}

(() => {
    var container = window.parent || window;
    container.Motio = container.Motio || {};
    container.Motio['/*FILENAME*/'] = new MotioAnimatedSvg();
})();
]]>