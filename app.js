'use strict';
const characters={
 howard:{name:'霍华德',en:'HOWARD',traits:'敏感 · 好奇 · 笨拙的冷幽默',description:'刚踏入大学的摄影学生，在与室友拉里的相处中学习怎样靠近别人。他敏感、好奇，也会借冷幽默掩饰尴尬；关于友谊和未来，他还没有现成的答案。',edit:'从宿舍的一次闲聊，到告别前的一段沉默，陪他走过尚未成为侦探的日子。',label:'他的故事'},
 clarissa:{name:'克拉丽莎',en:'CLARISSA',traits:'要强 · 敏锐 · 不肯退让',description:'身为家族的继承人，她很早就认识了权威与期待。父亲的目光、家族的规矩，以及不愿退让的自尊，推着她一步步争取自己的位置。',edit:'从童年时直接的追问，到成年后锋利的回应，看看她如何在家族的阴影里寻找掌控。',label:'她的故事'},
 renee:{name:'蕾妮',en:'RENEE',traits:'敏锐 · 清醒 · 寻找自己的位置',description:'曾经的记者，如今被日常生活包围。她依然观察、思考，也开始追问：在亲密关系和自己的抱负之间，究竟还有多少选择属于自己？',edit:'走进她的公寓，听见温柔与争执背后的犹疑，陪她重新审视自己想过的人生。',label:'她的故事'},
 eli:{name:'伊莱',en:'ELI',traits:'求知 · 审慎 · 直面未知',description:'一名面对未知发现的科学家。当实验结果动摇原有的解释，关于进步、责任与后果的问题，也逐渐变得无法回避。',edit:'在研究现场观察、交谈与实验，面对一个问题：我们所做的一切，真的都是为了更好的未来吗？',label:'他的故事'}
};
const tabs=[...document.querySelectorAll('[role="tab"]')];
let currentCharacter='howard';
const motionPreference=window.matchMedia('(prefers-reduced-motion: reduce)');
let animationPaused=motionPreference.matches;
function syncMotion(){const sprite=document.querySelector('#character-sprite');sprite.src='assets/'+currentCharacter+(animationPaused?'-idle-still.png':'-idle.webp');sprite.alt=characters[currentCharacter].name+(animationPaused?'的游戏内人物形象':'的游戏内待机动画');const toggle=document.querySelector('#motion-toggle');toggle.textContent=animationPaused?'播放人物动画':'暂停人物动画';toggle.setAttribute('aria-pressed',String(animationPaused));}
function selectCharacter(tab){const key=tab.dataset.character,c=characters[key];currentCharacter=key;tabs.forEach(t=>{const active=t===tab;t.setAttribute('aria-selected',String(active));t.tabIndex=active?0:-1;});document.querySelector('#character-en').textContent=c.en;document.querySelector('#character-name').textContent=c.name;document.querySelector('#character-traits').textContent=c.traits;document.querySelector('#character-description').textContent=c.description;document.querySelector('#character-edit').textContent=c.edit;document.querySelector('.editor-note span').textContent=c.label;document.querySelector('#character-panel').setAttribute('aria-labelledby',tab.id);const portrait=document.querySelector('#portrait');portrait.dataset.character=key;portrait.setAttribute('aria-label',c.name+'所在的游戏场景与动态人物');portrait.querySelector('.portrait-label').textContent='人物 / '+c.en;syncMotion();}
document.querySelector('#motion-toggle').addEventListener('click',()=>{animationPaused=!animationPaused;syncMotion();});
motionPreference.addEventListener('change',event=>{animationPaused=event.matches;syncMotion();});
syncMotion();
tabs.forEach((tab,index)=>{tab.addEventListener('click',()=>selectCharacter(tab));tab.addEventListener('keydown',event=>{let next;if(event.key==='ArrowRight')next=(index+1)%tabs.length;else if(event.key==='ArrowLeft')next=(index+tabs.length-1)%tabs.length;else if(event.key==='Home')next=0;else if(event.key==='End')next=tabs.length-1;else return;event.preventDefault();selectCharacter(tabs[next]);tabs[next].focus();});});
const dialog=document.querySelector('#feedback-dialog');let opener;
document.querySelectorAll('[data-feedback]').forEach(button=>button.addEventListener('click',()=>{opener=button;document.querySelector('#copy-status').textContent='';document.querySelector('#copy-email').textContent='复制邮箱';dialog.showModal();}));
document.querySelector('.dialog-close').addEventListener('click',()=>dialog.close());
dialog.addEventListener('click',event=>{if(event.target===dialog){const r=dialog.getBoundingClientRect();if(event.clientX<r.left||event.clientX>r.right||event.clientY<r.top||event.clientY>r.bottom)dialog.close();}});
dialog.addEventListener('close',()=>opener?.focus());
document.querySelector('#copy-email').addEventListener('click',async()=>{const email=document.querySelector('#feedback-email');try{await navigator.clipboard.writeText(email.value);document.querySelector('#copy-email').textContent='已复制';document.querySelector('#copy-status').textContent='邮箱已复制，可以粘贴到邮件客户端。';}catch{email.focus();email.select();document.querySelector('#copy-status').textContent='请复制已选中的邮箱地址。';}});
const menuToggle=document.querySelector('.menu-toggle'),menu=document.querySelector('#mobile-menu');menuToggle.addEventListener('click',()=>{const open=menuToggle.getAttribute('aria-expanded')!=='true';menuToggle.setAttribute('aria-expanded',String(open));menuToggle.setAttribute('aria-label',open?'收起导航':'展开导航');menu.hidden=!open;});menu.querySelectorAll('a').forEach(link=>link.addEventListener('click',()=>{menu.hidden=true;menuToggle.setAttribute('aria-expanded','false');menuToggle.setAttribute('aria-label','展开导航');}));
fetch('downloads/release.json?v=2026.11.27',{cache:'no-store'}).then(r=>r.ok?r.json():Promise.reject()).then(r=>{document.querySelector('#download-size').textContent=r.size_mb+' MB';}).catch(()=>{});
