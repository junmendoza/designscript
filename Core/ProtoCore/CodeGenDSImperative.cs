using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using ProtoCore.Utils;

namespace ProtoCore
{
    /// <summary>
    /// The code generator takes Abstract Syntax Tree and generates the DesignScript imperative code
    /// </summary>
    public class CodeGenDSImperative
    {
        public List<ProtoCore.AST.ImperativeAST.ImperativeNode> astNodeList { get; private set; }
        string code = string.Empty;

        public string Code { get { return code; } }

        /// <summary>
        /// This is used during ProtoAST generation to connect BinaryExpressionNode's 
        /// generated from Block nodes to its child AST tree - pratapa
        /// </summary>

        public CodeGenDSImperative(List<ProtoCore.AST.ImperativeAST.ImperativeNode> astList)
        {
            this.astNodeList = astList;
        }


        public CodeGenDSImperative() 
        {}
        
        /// <summary>
        /// This function prints the DS code into the destination stream
        /// </summary>
        /// <param name="code"></param>
        protected virtual void EmitCode(string code)
        {
            this.code += code;
        }

        public string GenerateCode()
        {
            Validity.Assert(null != astNodeList);
            
            for (int i = 0; i < astNodeList.Count; i++)
            {
                DFSTraverse(astNodeList[i]);
            }
            return code;
        }

        /// <summary>
        /// Depth first traversal of an AST node
        /// </summary>
        /// <param name="node"></param>
        public void DFSTraverse( ProtoCore.AST.Node node, bool useByProtoAst = false)
        {
            if (node is ProtoCore.AST.ImperativeAST.IdentifierNode)
            {
                EmitIdentifierNode(node as ProtoCore.AST.ImperativeAST.IdentifierNode);
            }
            else if (node is ProtoCore.AST.ImperativeAST.IdentifierListNode)
            {
                EmitIdentifierListNode(node as ProtoCore.AST.ImperativeAST.IdentifierListNode);
            }
            else if (node is ProtoCore.AST.ImperativeAST.IntNode)
            {
                EmitIntNode(node as ProtoCore.AST.ImperativeAST.IntNode);
            }
            else if (node is ProtoCore.AST.ImperativeAST.DoubleNode)
            {
                EmitDoubleNode(node as ProtoCore.AST.ImperativeAST.DoubleNode);
            }
            else if (node is ProtoCore.AST.ImperativeAST.FunctionCallNode)
            {
                EmitFunctionCallNode(node as ProtoCore.AST.ImperativeAST.FunctionCallNode);
            }
            else if (node is ProtoCore.AST.ImperativeAST.BinaryExpressionNode)
            {
                ProtoCore.AST.ImperativeAST.BinaryExpressionNode binaryExpr = node as ProtoCore.AST.ImperativeAST.BinaryExpressionNode;
                if (binaryExpr.Optr != DSASM.Operator.assign)
                    EmitCode("(");
                EmitBinaryNode(binaryExpr);
                if (binaryExpr.Optr == DSASM.Operator.assign)
                {
                    EmitCode(ProtoCore.DSASM.Constants.termline);
                }
                if (binaryExpr.Optr != DSASM.Operator.assign)
                    EmitCode(")");
            }
            else if (node is ProtoCore.AST.ImperativeAST.FunctionDefinitionNode)
            {
                EmitFunctionDefNode(node as ProtoCore.AST.ImperativeAST.FunctionDefinitionNode);
            }
            else if (node is ProtoCore.AST.ImperativeAST.NullNode)
            {
                EmitNullNode(node as ProtoCore.AST.ImperativeAST.NullNode);
            }
            else if (node is ProtoCore.AST.ImperativeAST.RangeExprNode)
            {
                EmitRangeExprNode(node as ProtoCore.AST.ImperativeAST.RangeExprNode);
            }
            else if (node is ProtoCore.AST.ImperativeAST.ArrayNode)
            {
                EmitArrayNode(node as ProtoCore.AST.ImperativeAST.ArrayNode);
            }
            else if (node is ProtoCore.AST.ImperativeAST.ExprListNode)
            {
                EmitExprListNode(node as ProtoCore.AST.ImperativeAST.ExprListNode);
            }
        }


        /// <summary>
        /// These functions emit the DesignScript code on the destination stream
        /// </summary>
        /// <param name="identNode"></param>
#region ASTNODE_CODE_EMITTERS

        private void EmitExprListNode(AST.ImperativeAST.ExprListNode exprListNode)
        {
            EmitCode("{");
            foreach (AST.ImperativeAST.ImperativeNode node in exprListNode.list)
            {
                DFSTraverse(node);
                EmitCode(",");
            }
            code = code.TrimEnd(',');
            EmitCode("}");
        }

        private void EmitArrayNode(AST.ImperativeAST.ArrayNode arrayNode)
        {
            if (null != arrayNode)
            {
                EmitCode("[");
                DFSTraverse(arrayNode.Expr);
                EmitCode("]");
                if (arrayNode.Type != null)
                {
                    DFSTraverse(arrayNode.Type);
                }
            }
        }


        private void EmitRangeExprNode(AST.ImperativeAST.RangeExprNode rangeExprNode)
        {
            Validity.Assert(null != rangeExprNode);
            if (rangeExprNode.FromNode is AST.ImperativeAST.IntNode)
                EmitCode((rangeExprNode.FromNode as AST.ImperativeAST.IntNode).value);
            else if (rangeExprNode.FromNode is AST.ImperativeAST.IdentifierNode)
                EmitCode((rangeExprNode.FromNode as AST.ImperativeAST.IdentifierNode).Value);
            EmitCode("..");

            if (rangeExprNode.ToNode is AST.ImperativeAST.IntNode)
                EmitCode((rangeExprNode.ToNode as AST.ImperativeAST.IntNode).value);
            else if (rangeExprNode.ToNode is AST.ImperativeAST.IdentifierNode)
                EmitCode((rangeExprNode.ToNode as AST.ImperativeAST.IdentifierNode).Value);

            if (rangeExprNode.StepNode != null)
            {
                EmitCode("..");
                if (rangeExprNode.stepoperator == ProtoCore.DSASM.RangeStepOperator.num)
                    EmitCode("#");
                if (rangeExprNode.StepNode is AST.ImperativeAST.IntNode)
                    EmitCode((rangeExprNode.StepNode as AST.ImperativeAST.IntNode).value);
                else if (rangeExprNode.StepNode is AST.ImperativeAST.IdentifierNode)
                    EmitCode((rangeExprNode.StepNode as AST.ImperativeAST.IdentifierNode).Value);
            }
        }

        protected virtual void EmitIdentifierNode(ProtoCore.AST.ImperativeAST.IdentifierNode identNode)
        {
            Validity.Assert(null != identNode);
            string identName = identNode.Value;
            EmitCode(identName);
            EmitArrayNode(identNode.ArrayDimensions);
        }

        protected virtual void EmitIdentifierListNode(ProtoCore.AST.ImperativeAST.IdentifierListNode identList)
        {
            Validity.Assert(null != identList);
            DFSTraverse(identList.LeftNode);
            EmitCode(".");
            DFSTraverse(identList.RightNode);
        }

        protected virtual void EmitIntNode(ProtoCore.AST.ImperativeAST.IntNode intNode)
        {
            Validity.Assert(null != intNode);
            EmitCode(intNode.value);
        }

        protected virtual void EmitDoubleNode(ProtoCore.AST.ImperativeAST.DoubleNode doubleNode)
        {
            Validity.Assert(null != doubleNode);
            EmitCode(doubleNode.value);
        }

        protected virtual void EmitFunctionCallNode(ProtoCore.AST.ImperativeAST.FunctionCallNode funcCallNode)
        {
            Validity.Assert(null != funcCallNode);

            Validity.Assert(funcCallNode.Function is ProtoCore.AST.ImperativeAST.IdentifierNode);
            string functionName = (funcCallNode.Function as ProtoCore.AST.ImperativeAST.IdentifierNode).Value;

            Validity.Assert(!string.IsNullOrEmpty(functionName));
            if (functionName.StartsWith("%"))
            {
                EmitCode("(");
                DFSTraverse(funcCallNode.FormalArguments[0], true);
                switch (functionName)
                {
                    case "%add":
                        EmitCode("+");
                        break;
                    case "%sub":
                        EmitCode("-");
                        break;
                    case "%mul":
                        EmitCode("*");
                        break;
                    case "%div":
                        EmitCode("/");
                        break;
                    case "%mod":
                        EmitCode("%");
                        break;
                    case "%Not":
                        EmitCode("!");
                        break;
                }

                if (funcCallNode.FormalArguments.Count > 1)
                {
                    DFSTraverse(funcCallNode.FormalArguments[1], true);
                }
                EmitCode(")");
            }
            else
            {
                EmitCode(functionName);

                EmitCode("(");
                for (int n = 0; n < funcCallNode.FormalArguments.Count; ++n) 
                {
                    ProtoCore.AST.ImperativeAST.ImperativeNode argNode = funcCallNode.FormalArguments[n];
                    DFSTraverse(argNode, true);
                    if (n+1 < funcCallNode.FormalArguments.Count)
                    {
                        EmitCode(",");
                    }
                }
                EmitCode(")");
            }
        }

        protected virtual void EmitBinaryNode(ProtoCore.AST.ImperativeAST.BinaryExpressionNode binaryExprNode)
        {
            Validity.Assert(null != binaryExprNode);
            DFSTraverse(binaryExprNode.LeftNode);
            EmitCode(ProtoCore.Utils.CoreUtils.GetOperatorString(binaryExprNode.Optr));
            DFSTraverse(binaryExprNode.RightNode);
        }

        protected virtual void EmitFunctionDefNode(ProtoCore.AST.ImperativeAST.FunctionDefinitionNode funcDefNode)
        {
            EmitCode("def ");
            EmitCode(funcDefNode.Name);

            if (funcDefNode.ReturnType.UID != ProtoCore.DSASM.Constants.kInvalidIndex)
            {
                EmitCode(": " + funcDefNode.ReturnType.Name);
            }

            if (funcDefNode.Signature != null)
            {
                EmitCode(funcDefNode.Signature.ToString());
            }
            else 
            {
                EmitCode("()\n");
            }
            
            if (null != funcDefNode.FunctionBody)
            {
                List<ProtoCore.AST.ImperativeAST.ImperativeNode> funcBody = funcDefNode.FunctionBody.Body;
            
                EmitCode("{\n");
                foreach (ProtoCore.AST.ImperativeAST.ImperativeNode bodyNode in funcBody)
                {
                    if (bodyNode is ProtoCore.AST.ImperativeAST.BinaryExpressionNode)
                        EmitBinaryNode(bodyNode as ProtoCore.AST.ImperativeAST.BinaryExpressionNode);
                    if (bodyNode is ProtoCore.AST.ImperativeAST.ReturnNode)
                        EmitReturnNode(bodyNode as ProtoCore.AST.ImperativeAST.ReturnNode);
                    EmitCode(";\n");
                }
                EmitCode("}");
            }
            EmitCode("\n");
        }

        protected virtual void EmitReturnNode(ProtoCore.AST.ImperativeAST.ReturnNode returnNode)
        {
            EmitCode("return = ");
            ProtoCore.AST.ImperativeAST.ImperativeNode rightNode = returnNode.ReturnExpr;
            DFSTraverse(rightNode);
        }

        protected virtual void EmitVarDeclNode(ProtoCore.AST.ImperativeAST.VarDeclNode varDeclNode)
        {
            EmitCode(varDeclNode.NameNode.Name + " : " + varDeclNode.ArgumentType.Name + ";\n");
        }
       
        protected virtual void EmitNullNode(ProtoCore.AST.ImperativeAST.NullNode nullNode)
        {
            Validity.Assert(null != nullNode);
            EmitCode("null");
        }

#endregion

    }
}
